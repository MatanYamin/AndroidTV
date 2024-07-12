namespace RemoteForAndroidTV;
using System;
using Microsoft.Maui.Controls;

public partial class MainRemote : ContentPage{

    RemoteButtons _remote;

    public MainRemote(RemoteButtons remote){

        InitializeComponent();

        _remote = remote;
    }

    private void OnBTN1(object sender, EventArgs e)
    {
        _remote.TestChannelUpCommand();

    }
    private void OnBTN2(object sender, EventArgs e)
    {
         _remote.Digit1();
    }

    private async void SwitchDevice(object sender, EventArgs e)
    {
        _remote.CleanRemote();

        await Navigation.PopToRootAsync();

    }

}