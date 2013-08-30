using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Auth0.SDK
{
    public partial class LoginBrowser : UserControl
    {
        public Uri StartUrl { get; set; }
        public Uri EndUrl { get; set; }

        public LoginBrowser(Uri startUrl, Uri endUrl)
        {
            InitializeComponent();
            this.StartUrl = startUrl;
            this.EndUrl = endUrl;
        }
    }
}
