/* VBWR B
 * Project: MarkVibe
 * Repository: https://github.com/umbertotechnopreneur/MarkVibe
 * Creator: Umberto Giacobbi | https://umbertogiacobbi.biz
 * VibeWare: Human intent, AI execution, and plenty of tokens
 * Manifesto: https://umbertogiacobbi.biz/vibeware/manifesto
 * Created with AI: OpenAI Codex; optional Markdown default-app setup; 2026-10-25
 * Human guidance: Umberto Giacobbi; ask whether to associate Markdown during installation
 * Evidence: AI-DEVELOPMENT-DISCLOSURE.md; .github/tasks/archive.md
 * Copyright (c) 2026 Umberto Giacobbi
 * License: MIT (see LICENSE)
 * VBWR E */
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MarkVibe.Core;
using Microsoft.Win32;
using static MarkVibe.Core.UiStrings;

namespace MarkVibe.Windows;

/// <summary>Offers installed users Windows-controlled Markdown defaults once per installation identity.</summary>
internal static class MarkdownFileAssociations
{
    /// <summary>Identifies the installed handler and the Windows query used to recognize it.</summary>
    private sealed record Registration(string SettingsUri, string DefaultIdentifier, uint AssociationString);

    /// <summary>Reads effective per-extension defaults without changing Windows or the remembered offer.</summary>
    internal static FileAssociationStatus GetStatus(DesktopOptions options)
    {
        if (options.SmokeTest || File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")))
        {
            return new(false, null, null);
        }

        var registration = GetRegistration();
        return registration == null
            ? new(false, null, null)
            : new(true, IsDefault(".md", registration), IsDefault(".markdown", registration));
    }

    /// <summary>Opens Windows setup on an explicit Settings action, independently of the one-time offer.</summary>
    internal static void OpenSettings(DesktopOptions options)
    {
        if (options.SmokeTest || File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")))
        {
            throw new InvalidOperationException("Markdown registration is unavailable in this application mode.");
        }

        if (GetRegistration() is not { } registration)
        {
            throw new InvalidOperationException("This installed application has no Markdown registration.");
        }

        Process.Start(new ProcessStartInfo(registration.SettingsUri) { UseShellExecute = true });
    }

    /// <summary>Compares the effective packaged AppUserModelID or MSI ProgID without guessing from app names.</summary>
    private static bool? IsDefault(string extension, Registration registration)
    {
        uint characterCount = 0;
        var result = AssocQueryString(0, registration.AssociationString, extension, null, null, ref characterCount);
        const int noAssociation = unchecked((int)0x80070483);
        const int fileNotFound = unchecked((int)0x80070002);
        if (result is noAssociation or fileNotFound)
        {
            return false;
        }

        if (result != 1 || characterCount is 0 or > 32768)
        {
            return null;
        }

        var identifier = new StringBuilder((int)characterCount);
        result = AssocQueryString(0, registration.AssociationString, extension, null, identifier, ref characterCount);
        return result == 0
            ? string.Equals(identifier.ToString(), registration.DefaultIdentifier, StringComparison.OrdinalIgnoreCase)
            : null;
    }

    /// <summary>Asks once, defaults to No, and opens Windows Settings only after an explicit Yes.</summary>
    internal static void Offer(DesktopOptions options, Window? owner = null)
    {
        if (options.SmokeTest || File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag")))
        {
            return;
        }

        if (owner == null)
        {
            UiStrings.SetLanguage("system", CultureInfo.CurrentUICulture.Name);
        }

        try
        {
            var registration = GetRegistration();
            if (registration == null)
            {
                // Source builds and portable copies do not own an installed file registration.
                return;
            }

            var settingsUri = registration.SettingsUri;
            var identityHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(settingsUri)));
            Directory.CreateDirectory(options.DataDirectory);
            var markerPath = Path.Combine(options.DataDirectory, "markdown-association-" + identityHash);

            // An exclusive handle prevents simultaneous installer/startup processes from asking twice.
            using var marker = new FileStream(
                markerPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (marker.Length != 0)
            {
                return;
            }

            // MSI writes handler registrations directly. Tell the shell to refresh its association cache.
            const int associationChanged = 0x08000000;
            SHChangeNotify(associationChanged, 0, IntPtr.Zero, IntPtr.Zero);

            var message = T("Would you like to use MarkVibe to open Markdown files (.md and .markdown)? Choose Yes to open Windows Settings, then select MarkVibe for these file types.");
            var choice = owner == null
                ? MessageBox.Show(
                    message, "MarkVibe", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                : MessageBox.Show(
                    owner, message, "MarkVibe", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            if (choice == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(settingsUri) { UseShellExecute = true });
            }

            // This records that the offer was answered, not that Windows changed its defaults.
            marker.WriteByte(1);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            System.Security.SecurityException or Win32Exception or InvalidOperationException)
        {
            DesktopLog.Error("fileAssociations.offerFailed", error);
            if (options.ConfigureFileAssociations)
            {
                MessageBox.Show(
                    T("Default-app setup could not be opened. Open Windows Settings > Apps > Default apps and select MarkVibe."),
                    "MarkVibe", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    /// <summary>Finds this package or this exact MSI executable's registered Default Apps entry.</summary>
    private static Registration? GetRegistration()
    {
        uint familyNameLength = 0;
        const int insufficientBuffer = 122;
        const int noPackage = 15700;
        var identityResult = GetCurrentPackageFamilyName(ref familyNameLength, null);
        if (identityResult == insufficientBuffer)
        {
            var familyName = new StringBuilder((int)familyNameLength);
            var result = GetCurrentPackageFamilyName(ref familyNameLength, familyName);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            var applicationId = familyName + "!MarkVibe";
            const uint appUserModelId = 21; // ASSOCSTR_APPID, the effective default's application identity.
            return new(
                "ms-settings:defaultapps?registeredAUMID=" + Uri.EscapeDataString(applicationId),
                applicationId,
                appUserModelId);
        }

        if (identityResult != noPackage)
        {
            throw new Win32Exception(identityResult);
        }

#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        var registrationName = $"MarkVibe.{configuration}.{architecture}";
        using var applications = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications");
        if (applications?.GetValue(registrationName) is not string capabilitiesPath)
        {
            return null;
        }

        using var capabilities = Registry.CurrentUser.OpenSubKey(capabilitiesPath);
        var expectedIcon = $"\"{Environment.ProcessPath}\",0";
        if (!string.Equals(
            capabilities?.GetValue("ApplicationIcon") as string,
            expectedIcon,
            StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        const uint progId = 20; // ASSOCSTR_PROGID, the effective default's registered handler.
        return new(
            "ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(registrationName),
            registrationName + ".Markdown",
            progId);
    }

    /// <summary>Reads a shell association string, including the Windows user's effective default choice.</summary>
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryString(
        uint flags, uint associationString, string association, string? extra,
        StringBuilder? output, ref uint characterCount);

    /// <summary>Queries package identity without treating an unpackaged desktop process as a failure.</summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(
        ref uint packageFamilyNameLength, StringBuilder? packageFamilyName);

    /// <summary>Refreshes Explorer's association cache after MSI has registered the available handler.</summary>
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr firstItem, IntPtr secondItem);
}
