using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AboutBoxWpf
{
    class About : AboutControlViewModel
    {
        public void Show()
        {
            AboutControlView about = new AboutControlView();
            AboutControlViewModel vm = (AboutControlViewModel)about.FindResource("ViewModel");
            vm.AdditionalNotes = this.AdditionalNotes;
            vm.ApplicationLogo = this.ApplicationLogo;
            vm.Copyright = this.Copyright;
            vm.Description = this.Description;
            vm.HyperlinkText = this.HyperlinkText;
            vm.Publisher = this.Publisher;
            vm.PublisherLogo = this.PublisherLogo;
            vm.Title = this.Title;
            vm.Version = this.Version;

            vm.Window.Content = about;
            vm.Window.Show();
        }
    }
}
