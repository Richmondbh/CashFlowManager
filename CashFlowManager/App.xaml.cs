using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;

namespace CashFlowManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set Swedish culture globally so all currency formatting shows kr
            CultureInfo swedishCulture = new CultureInfo("sv-SE");
            Thread.CurrentThread.CurrentCulture = swedishCulture;
            Thread.CurrentThread.CurrentUICulture = swedishCulture;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(swedishCulture.IetfLanguageTag)));

            base.OnStartup(e);
        }

    }
}
