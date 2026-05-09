using System.Diagnostics;
using System.Text.Json;
using AutoLogin.App.Models;
using AutoLogin.App.Services.Browser;
using AutoLogin.App.Services.Security;
using OtpNet;

namespace AutoLogin.App.Services.Automation;

public sealed class BrowserAutomationEngine : IAutomationEngine
{
    private readonly ICredentialProtector _credentialProtector;

    public BrowserAutomationEngine(ICredentialProtector credentialProtector)
    {
        _credentialProtector = credentialProtector;
    }

    public async Task<AutomationExecutionResult> ExecuteAsync(
        IBrowserSession browserSession,
        LoginEntry entry,
        AutomationProfile profile,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await browserSession.NavigateAsync(entry.StartUrl, cancellationToken);
            await browserSession.WaitForDocumentReadyAsync(TimeSpan.FromSeconds(20), cancellationToken);

            var password = _credentialProtector.Unprotect(entry.EncryptedPassword);
            var totpCode = BuildTotpCode(entry);

            for (var index = 0; index < profile.Steps.Count; index++)
            {
                var step = profile.Steps[index];
                await ExecuteStepAsync(browserSession, step, index, entry, password, totpCode, profile, cancellationToken);
            }

            stopwatch.Stop();
            return new AutomationExecutionResult
            {
                IsSuccess = true,
                Message = "Automation erfolgreich abgeschlossen.",
                Elapsed = stopwatch.Elapsed
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new AutomationExecutionResult
            {
                IsSuccess = false,
                FailedStepIndex = TryExtractStepIndex(exception),
                Message = exception.Message,
                Elapsed = stopwatch.Elapsed
            };
        }
    }

    private async Task ExecuteStepAsync(
        IBrowserSession browserSession,
        AutomationStep step,
        int stepIndex,
        LoginEntry entry,
        string password,
        string? totpCode,
        AutomationProfile profile,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMilliseconds(step.TimeoutMs ?? profile.TimeoutMs);

        try
        {
            switch (step.Action.Trim().ToLowerInvariant())
            {
                case "wait_for_selector":
                    await WaitForSelectorAsync(browserSession, step.Selector, timeout, cancellationToken);
                    break;

                case "wait_for_text":
                    await WaitForTextAsync(browserSession, step.Value, timeout, cancellationToken);
                    break;

                case "set_value":
                    await SetValueAsync(
                        browserSession,
                        step.Selector,
                        ResolveValue(step.Value, entry.Username, password, totpCode),
                        timeout,
                        cancellationToken);
                    break;

                case "set_totp":
                    if (string.IsNullOrWhiteSpace(totpCode))
                    {
                        throw new InvalidOperationException("Für diesen Login ist kein TOTP-Secret hinterlegt.");
                    }

                    await SetValueAsync(
                        browserSession,
                        step.Selector,
                        totpCode,
                        timeout,
                        cancellationToken);
                    break;

                case "click":
                    await ClickAsync(browserSession, step.Selector, timeout, cancellationToken);
                    if (profile.WaitForNavigation)
                    {
                        await browserSession.WaitForDocumentReadyAsync(timeout, cancellationToken);
                    }

                    break;

                case "click_text":
                    await ClickByTextAsync(browserSession, step.Value, timeout, cancellationToken);
                    if (profile.WaitForNavigation)
                    {
                        await browserSession.WaitForDocumentReadyAsync(timeout, cancellationToken);
                    }

                    break;

                case "submit_form":
                    if (entry.AutoSubmit)
                    {
                        await SubmitFormAsync(browserSession, step.Selector, timeout, cancellationToken);
                        if (profile.WaitForNavigation)
                        {
                            await browserSession.WaitForDocumentReadyAsync(timeout, cancellationToken);
                        }
                    }

                    break;

                case "delay":
                    await Task.Delay(step.DelayMs ?? 500, cancellationToken);
                    break;

                case "assert_url":
                    await AssertUrlAsync(browserSession, step.UrlPattern, timeout, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unbekannter Automationsschritt: '{step.Action}'.");
            }
        }
        catch (Exception exception)
        {
            throw new AutomationStepException(stepIndex, exception);
        }
    }

    private string? BuildTotpCode(LoginEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.EncryptedTotpSecret))
        {
            return null;
        }

        var secret = _credentialProtector.Unprotect(entry.EncryptedTotpSecret);
        var normalizedSecret = secret
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var secretBytes = Base32Encoding.ToBytes(normalizedSecret);
        return new Totp(secretBytes).ComputeTotp();
    }

    private static int? TryExtractStepIndex(Exception exception)
    {
        if (exception is AutomationStepException stepException)
        {
            return stepException.StepIndex;
        }

        return exception.InnerException is AutomationStepException innerStepException
            ? innerStepException.StepIndex
            : null;
    }

    private static string ResolveValue(string? value, string username, string password, string? totpCode)
    {
        return (value ?? string.Empty)
            .Replace("{{username}}", username, StringComparison.Ordinal)
            .Replace("{{password}}", password, StringComparison.Ordinal)
            .Replace("{{totp}}", totpCode ?? string.Empty, StringComparison.Ordinal);
    }

    private static async Task WaitForSelectorAsync(
        IBrowserSession browserSession,
        string? selector,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var found = await EvaluateAsync<bool>(
                browserSession,
                $"Boolean(document.querySelector({ToJsLiteral(selector)}))",
                cancellationToken);

            if (found)
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Selector wurde nicht gefunden: {selector}");
    }

    private static async Task WaitForTextAsync(
        IBrowserSession browserSession,
        string? expectedText,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedText);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var found = await EvaluateAsync<bool>(
                browserSession,
                $$"""
                (() => {
                    const text = (document.body?.innerText || document.body?.textContent || '');
                    return text.includes({{ToJsLiteral(expectedText)}});
                })()
                """,
                cancellationToken);

            if (found)
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Text wurde nicht gefunden: {expectedText}");
    }

    private static async Task SetValueAsync(
        IBrowserSession browserSession,
        string? selector,
        string value,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await WaitForSelectorAsync(browserSession, selector, timeout, cancellationToken);

        var success = await EvaluateAsync<bool>(
            browserSession,
            $$"""
            (() => {
                const element = document.querySelector({{ToJsLiteral(selector)}});
                if (!element) { return false; }
                const activate = () => {
                    if (element.scrollIntoView) {
                        element.scrollIntoView({ block: 'center', inline: 'center' });
                    }
                    element.dispatchEvent(new MouseEvent('pointerdown', { bubbles: true }));
                    element.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                    if (typeof element.click === 'function') {
                        element.click();
                    }
                    element.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                    element.focus();
                    if (typeof element.select === 'function') {
                        element.select();
                    }
                    if (typeof element.setSelectionRange === 'function') {
                        element.setSelectionRange(0, element.value?.length ?? 0);
                    }
                };

                const setNativeValue = (target, nextValue) => {
                    const prototype = target instanceof HTMLTextAreaElement
                        ? HTMLTextAreaElement.prototype
                        : HTMLInputElement.prototype;
                    const descriptor = Object.getOwnPropertyDescriptor(prototype, 'value');
                    if (descriptor && descriptor.set) {
                        descriptor.set.call(target, nextValue);
                    } else {
                        target.value = nextValue;
                    }
                };

                activate();

                if (document.activeElement !== element) {
                    activate();
                }

                setNativeValue(element, '');
                element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'deleteContentBackward', data: null }));

                const fullValue = {{ToJsLiteral(value)}};
                let currentValue = '';
                for (const character of fullValue) {
                    activate();
                    element.dispatchEvent(new KeyboardEvent('keydown', { key: character, bubbles: true }));
                    currentValue += character;
                    setNativeValue(element, currentValue);
                    element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: character }));
                    element.dispatchEvent(new KeyboardEvent('keyup', { key: character, bubbles: true }));
                }

                if (element.scrollIntoView) {
                    element.scrollIntoView({ block: 'center', inline: 'center' });
                }
                element.dispatchEvent(new Event('change', { bubbles: true }));
                return document.activeElement === element && element.value === fullValue;
            })()
            """,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException($"Wert konnte nicht gesetzt werden: {selector}");
        }
    }

    private static async Task ClickAsync(
        IBrowserSession browserSession,
        string? selector,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await WaitForSelectorAsync(browserSession, selector, timeout, cancellationToken);

        var success = await EvaluateAsync<bool>(
            browserSession,
            $$"""
            (() => {
                const element = document.querySelector({{ToJsLiteral(selector)}});
                if (!element) { return false; }
                if (element.scrollIntoView) {
                    element.scrollIntoView({ block: 'center', inline: 'center' });
                }
                element.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                element.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                element.click();
                return true;
            })()
            """,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException($"Klick fehlgeschlagen: {selector}");
        }
    }

    private static async Task ClickByTextAsync(
        IBrowserSession browserSession,
        string? expectedText,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedText);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var success = await EvaluateAsync<bool>(
                browserSession,
                $$"""
                (() => {
                    const targetText = {{ToJsLiteral(expectedText)}}.trim().toLowerCase();
                    const candidates = Array.from(document.querySelectorAll('button, a, input[type="button"], input[type="submit"], div[role="button"], span[role="button"]'));
                    const element = candidates.find(candidate => {
                        const text = (candidate.innerText || candidate.textContent || candidate.value || '').trim().toLowerCase();
                        return text === targetText || text.includes(targetText);
                    });

                    if (!element) { return false; }
                    if (element.scrollIntoView) {
                        element.scrollIntoView({ block: 'center', inline: 'center' });
                    }
                    element.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                    element.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                    if (typeof element.click === 'function') {
                        element.click();
                    }
                    return true;
                })()
                """,
                cancellationToken);

            if (success)
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"Klicktext wurde nicht gefunden: {expectedText}");
    }

    private static async Task SubmitFormAsync(
        IBrowserSession browserSession,
        string? selector,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(selector))
        {
            await WaitForSelectorAsync(browserSession, selector, timeout, cancellationToken);
        }

        var anchorExpression = string.IsNullOrWhiteSpace(selector)
            ? "null"
            : $"document.querySelector({ToJsLiteral(selector)})";

        var success = await EvaluateAsync<bool>(
            browserSession,
            $$"""
            (() => {
                const anchor = {{anchorExpression}};
                const form = anchor?.closest('form') ?? document.querySelector('form');
                const textMatchers = ['log on', 'logon', 'login', 'log in', 'sign in', 'anmelden'];
                const matchesText = (element) => {
                    const text = (element.innerText || element.textContent || element.value || '').trim().toLowerCase();
                    return textMatchers.includes(text);
                };
                const clickCandidate = (element) => {
                    if (!element) { return false; }
                    if (element.scrollIntoView) {
                        element.scrollIntoView({ block: 'center', inline: 'center' });
                    }
                    element.dispatchEvent(new MouseEvent('mousedown', { bubbles: true }));
                    element.dispatchEvent(new MouseEvent('mouseup', { bubbles: true }));
                    element.click();
                    return true;
                };

                const scopedRoot = form || document;
                const explicitSubmitButton =
                    scopedRoot.querySelector("button[type='submit'], input[type='submit'], input[type='button'][value='Log On'], .authentication-button, .ns-logon-button");
                if (clickCandidate(explicitSubmitButton)) {
                    return true;
                }

                const textButton = Array.from(scopedRoot.querySelectorAll('button, input[type=\"submit\"], input[type=\"button\"], a'))
                    .find(matchesText);
                if (clickCandidate(textButton)) {
                    return true;
                }

                if (!form) { return false; }

                const passwordField = anchor?.matches?.("input[type='password']") ? anchor : form.querySelector("input[type='password']");
                if (passwordField) {
                    passwordField.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', bubbles: true }));
                    passwordField.dispatchEvent(new KeyboardEvent('keypress', { key: 'Enter', code: 'Enter', bubbles: true }));
                    passwordField.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', bubbles: true }));
                }

                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
                return true;
            })()
            """,
            cancellationToken);

        if (!success)
        {
            throw new InvalidOperationException("Kein Formular zum Absenden gefunden.");
        }
    }

    private static async Task AssertUrlAsync(
        IBrowserSession browserSession,
        string? urlPattern,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(urlPattern);

        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var currentUrl = await browserSession.GetCurrentUrlAsync(cancellationToken);
            if (currentUrl.Contains(urlPattern, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException($"URL-Muster wurde nicht erreicht: {urlPattern}");
    }

    private static async Task<T> EvaluateAsync<T>(
        IBrowserSession browserSession,
        string script,
        CancellationToken cancellationToken)
    {
        var json = await browserSession.ExecuteScriptAsync(script, cancellationToken);
        var value = JsonSerializer.Deserialize<T>(json);
        if (value is null)
        {
            throw new InvalidOperationException("Script-Ergebnis konnte nicht ausgewertet werden.");
        }

        return value;
    }

    private static string ToJsLiteral(string? value)
    {
        return JsonSerializer.Serialize(value ?? string.Empty);
    }

    private sealed class AutomationStepException : Exception
    {
        public AutomationStepException(int stepIndex, Exception innerException)
            : base(innerException.Message, innerException)
        {
            StepIndex = stepIndex;
        }

        public int StepIndex { get; }
    }
}
