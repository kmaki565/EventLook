using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;
using System.Windows;
using System;

namespace AboutBoxWpf;

public class AboutControlViewModel : INotifyPropertyChanged
{
    private ImageSource _ApplicationLogo;
    private string _Title;
    private string _Description;
    private string _Version;
    private ImageSource _PublisherLogo;
    private string _Copyright;
    private string _AdditionalNotes;
    private string _HyperlinkText;
    private Uri _Hyperlink;
    private string _Publisher;
    private bool _isSemanticVersioning;

    public AboutControlViewModel()
    {
        Window = new Window();
        Window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Window.SizeToContent = SizeToContent.WidthAndHeight;
        Window.ResizeMode = ResizeMode.NoResize;
        Window.WindowStyle = WindowStyle.ToolWindow;

        Window.ShowInTaskbar = false;
        Window.Title = "About ";

        Assembly assembly = Assembly.GetEntryAssembly();
        Version = assembly.GetName().Version.ToString();
        Title = assembly.GetName().Name;

#if NET35 || NET40
			AssemblyCopyrightAttribute copyright = Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;
			AssemblyDescriptionAttribute description = Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute)) as AssemblyDescriptionAttribute;
			AssemblyCompanyAttribute company = Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute)) as AssemblyCompanyAttribute;
#else
        AssemblyCopyrightAttribute copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
        AssemblyDescriptionAttribute description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
        AssemblyCompanyAttribute company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
#endif
        Copyright = copyright.Copyright;
        Description = description.Description;
        Publisher = company.Company;

        AdditionalNotes = "Further information about ... InformationInformationInformationInformationInformationInformationInformationInformation";
    }

    /// <summary>
    /// Gets or sets the application logo.
    /// </summary>
    /// <value>The application logo.</value>
    public ImageSource ApplicationLogo
    {
        get
        {
            return _ApplicationLogo;
        }
        set
        {
            if (_ApplicationLogo != value)
            {
                _ApplicationLogo = value;
                OnPropertyChanged("ApplicationLogo");
            }
        }
    }

    /// <summary>
    /// Gets or sets the application title.
    /// </summary>
    /// <value>The application title.</value>
    public string Title
    {
        get
        {
            return _Title;
        }
        set
        {
            if (_Title != value)
            {
                _Title = value;
                Window.Title += value;
                OnPropertyChanged("Title");
            }
        }
    }

    /// <summary>
    /// Gets or sets the application info.
    /// </summary>
    /// <value>The application info.</value>
    public string Description
    {
        get
        {
            return _Description;
        }
        set
        {
            if (_Description != value)
            {
                _Description = value;
                OnPropertyChanged("Description");
            }
        }
    }

    /// <summary>
    /// Gets or sets if Semantic Versioning is used.
    /// </summary>
    /// <see cref="http://semver.org/"/>
    /// <value>The bool that indicats whether Semantic Versioning is used.</value>
    public bool IsSemanticVersioning
    {
        get
        {
            return _isSemanticVersioning;
        }
        set
        {
            _isSemanticVersioning = value;
            OnPropertyChanged("Version");
        }
    }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    /// <value>The application version.</value>
    public string Version
    {
        get
        {
            if (IsSemanticVersioning)
            {
                var tmp = _Version.Split('.');
                var version = string.Format("{0}.{1}.{2}.{3}", tmp[0], tmp[1], tmp[2], tmp[3]);
                return version;
            }

            return _Version;
        }
        set
        {
            if (_Version != value)
            {
                _Version = value;
                OnPropertyChanged("Version");
            }
        }
    }

    /// <summary>
    /// Gets or sets the publisher logo.
    /// </summary>
    /// <value>The publisher logo.</value>
    public ImageSource PublisherLogo
    {
        get
        {
            return _PublisherLogo;
        }
        set
        {
            if (_PublisherLogo != value)
            {
                _PublisherLogo = value;
                OnPropertyChanged("PublisherLogo");
            }
        }
    }

    /// <summary>
    /// Gets or sets the publisher.
    /// </summary>
    /// <value>The publisher.</value>
    public string Publisher
    {
        get
        {
            return _Publisher;
        }
        set
        {
            if (_Publisher != value)
            {
                _Publisher = value;
                OnPropertyChanged("Publisher");
            }
        }
    }

    /// <summary>
    /// Gets or sets the copyright label.
    /// </summary>
    /// <value>The copyright label.</value>
    public string Copyright
    {
        get
        {
            return _Copyright;
        }
        set
        {
            if (_Copyright != value)
            {
                _Copyright = value;
                OnPropertyChanged("Copyright");
            }
        }
    }

    /// <summary>
    /// Gets or sets the hyperlink text.
    /// </summary>
    /// <value>The hyperlink text.</value>
    public string HyperlinkText
    {
        get
        {
            return _HyperlinkText;
        }
        set
        {
            try
            {
                Hyperlink = new Uri(value);
                _HyperlinkText = value;
                OnPropertyChanged("HyperlinkText");
            }
            catch
            {
            }
        }
    }

    public Uri Hyperlink
    {
        get
        {
            return _Hyperlink;
        }
        set
        {
            if (_Hyperlink != value)
            {
                _Hyperlink = value;
                OnPropertyChanged("Hyperlink");
            }
        }
    }

    /// <summary>
    /// Gets or sets the further info.
    /// </summary>
    /// <value>The further info.</value>
    public string AdditionalNotes
    {
        get
        {
            return _AdditionalNotes;
        }
        set
        {
            if (_AdditionalNotes != value)
            {
                _AdditionalNotes = value;
                OnPropertyChanged("AdditionalNotes");
            }
        }
    }

    public Window Window
    {
        get;
        set;
    }

    /// <summary>
    /// Called when a property value has changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that has changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null)
        {
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}