﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using News.ViewModels;
using News.Helpers;
using Microsoft.MobCAT.Forms.Pages;

namespace News.Pages
{
    public partial class HomePage :  BaseContentPage<HomeViewModel>
    {
        public HomePage()
        {
            InitializeComponent();
        }

        void Handle_ItemSelected(object sender, Xamarin.Forms.SelectedItemChangedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
            // TODO: navigate to arcitle details
        }
    }
}
