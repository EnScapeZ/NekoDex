using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NekoDex;

/// <summary>Wechselt die Oberflächensprache zur Laufzeit und liefert die Texte aus <c>Resources/Strings.resx</c>.</summary>
public sealed class LanguageManager : INotifyPropertyChanged
{
    /// <summary>Muss vor <see cref="Instance"/> stehen. Der Instanz-Konstruktor liest die Systemkultur.</summary>
    private static readonly CultureInfo SystemUi = CaptureSystemCulture();

    private static LanguageManager? _instance;

    private readonly ResourceManager? _resources;

    private CultureInfo _culture;

    static LanguageManager()
    {
        try
        {
            _instance = new LanguageManager();
        }
        catch (Exception ex) when (ex is NullReferenceException or CultureNotFoundException or MissingManifestResourceException)
        {
            _instance = new LanguageManager(bare: true);
        }
        catch
        {
            _instance = new LanguageManager(bare: true);
        }
    }

    private LanguageManager()
        : this(bare: false)
    {
    }

    private LanguageManager(bool bare)
    {
        _resources = bare ? null : TryCreateResources();
        _culture = SafeResolve("auto");
    }

    public static LanguageManager Instance => _instance ??= new LanguageManager(bare: true);

    /// <summary>Vor dem Hauptfenster aufrufen, damit der XAML-Parser keine Typinitialisierung mehr anstößt.</summary>
    public static void EnsureInitialized()
    {
        try
        {
            _ = Instance;
        }
        catch
        {
            _instance ??= new LanguageManager(bare: true);
        }
    }

    public static bool IsInDesignMode
    {
        get
        {
            try
            {
                return DesignerProperties.GetIsInDesignMode(new DependencyObject());
            }
            catch
            {
                return false;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? Changed;

    public CultureInfo Culture => _culture;

    public string this[string key] => GetString(key) ?? key;

    public string? GetString(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;
        if (IsInDesignMode)
            return key;

        try
        {
            if (_resources is null)
                return key;

            return _resources.GetString(key, _culture)
                   ?? _resources.GetString(key, CultureInfo.InvariantCulture)
                   ?? _resources.GetString(key)
                   ?? key;
        }
        catch (Exception ex) when (ex is NullReferenceException or MissingManifestResourceException or InvalidOperationException)
        {
            return key;
        }
        catch
        {
            return key;
        }
    }

    public string Get(string key, params object[] args)
    {
        var value = GetString(key) ?? key;
        if (args is not { Length: > 0 })
            return value;

        try
        {
            return string.Format(_culture, value, args);
        }
        catch (Exception ex) when (ex is FormatException or NullReferenceException)
        {
            return value;
        }
    }

    public void Apply(string? preference) => SetCulture(SafeResolve(preference));

    public void SetCulture(CultureInfo culture)
    {
        _culture = culture ?? CultureInfo.InvariantCulture;
        try
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _culture;
            CultureInfo.DefaultThreadCurrentCulture = _culture;
            CultureInfo.DefaultThreadCurrentUICulture = _culture;
            if (Application.Current?.MainWindow is FrameworkElement window)
                window.Language = XmlLanguage.GetLanguage(_culture.IetfLanguageTag);
        }
        catch
        {
            _culture = CultureInfo.InvariantCulture;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        Changed?.Invoke();
    }

    /// <summary>Sprachen in der Einstellungs-Liste, jeweils mit dem Namen in der eigenen Sprache.</summary>
    public static IReadOnlyList<(string Code, string NativeName)> Choices { get; } =
    [
        ("en", "English"),
        ("de", "Deutsch"),
        ("fr", "Français"),
        ("es", "Español"),
        ("it", "Italiano"),
        ("pt", "Português"),
        ("nl", "Nederlands"),
        ("pl", "Polski"),
        ("sv", "Svenska"),
        ("da", "Dansk"),
        ("nb", "Norsk"),
        ("fi", "Suomi"),
        ("cs", "Čeština"),
        ("sk", "Slovenčina"),
        ("hu", "Magyar"),
        ("ro", "Română"),
        ("bg", "Български"),
        ("el", "Ελληνικά"),
        ("hr", "Hrvatski"),
        ("sl", "Slovenščina"),
        ("uk", "Українська"),
        ("ru", "Русский"),
        ("tr", "Türkçe"),
        ("zh-Hans", "中文"),
        ("ja", "日本語"),
        ("ko", "한국어"),
    ];

    public static bool IsSupported(string? code) => Canonical(code) is { } found && found.Length > 0;

    public static string? Canonical(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var key = code.Trim().Replace('_', '-');
        if (key.Equals("auto", StringComparison.OrdinalIgnoreCase) || key.Equals("system", StringComparison.OrdinalIgnoreCase))
            return "auto";
        if (key.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            return "zh-Hans";
        if (key.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
            return "pt";
        if (key.StartsWith("nb", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("nn", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("no", StringComparison.OrdinalIgnoreCase))
            return "nb";

        var two = key.Length >= 2 ? key[..2] : key;
        foreach (var (choice, _) in Choices)
        {
            if (choice.Equals(key, StringComparison.OrdinalIgnoreCase) || choice.Equals(two, StringComparison.OrdinalIgnoreCase))
                return choice;
        }

        return null;
    }

    public static CultureInfo Resolve(string? preference) => SafeResolve(preference);

    private static CultureInfo SafeResolve(string? preference)
    {
        try
        {
            var key = Canonical(preference);
            if (key is null or "auto")
                key = MatchSystem();
            return CultureOrInvariant(key);
        }
        catch (Exception ex) when (ex is NullReferenceException or CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static string MatchSystem()
    {
        var ui = SystemUi ?? CultureInfo.InvariantCulture;
        return Canonical(ui.Name) is { } full && full != "auto"
            ? full
            : Canonical(ui.TwoLetterISOLanguageName) is { } two && two != "auto"
                ? two
                : "en";
    }

    private static CultureInfo CultureOrInvariant(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static CultureInfo CaptureSystemCulture()
    {
        try
        {
            return CultureInfo.CurrentUICulture ?? CultureInfo.InstalledUICulture ?? CultureInfo.InvariantCulture;
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    private static ResourceManager? TryCreateResources()
    {
        try
        {
            return new ResourceManager("NekoDex.Resources.Strings", typeof(LanguageManager).Assembly);
        }
        catch
        {
            return null;
        }
    }
}
