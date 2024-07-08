namespace RemoteForAndroidTV;
using System;
using Microsoft.Maui.Controls;

public partial class MainRemote : ContentPage{

    SendCommands _commands;

    public MainRemote(string ip){

        InitializeComponent();
        _commands = new SendCommands(ip);

    }

    private async void OnBTN1(object sender, EventArgs e)
    {
        await _commands.TestChannelUpCommand();

    }
    private async void OnBTN2(object sender, EventArgs e)
    {
        // await _commands.test2();
        await _commands.TestVolumeCommand();
    }

}