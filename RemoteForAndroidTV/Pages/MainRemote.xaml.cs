namespace RemoteForAndroidTV;
using System;
using Microsoft.Maui.Controls;

public partial class MainRemote : ContentPage{

    readonly SendCommands _commands;

    EnterCodePage _enterCodePage;
    // HERE THE EVENT
    public MainRemote(string ip, EnterCodePage ecp){

        InitializeComponent();
        this._enterCodePage = ecp;
        this._commands = new SendCommands(ip);

    }

    public async Task InitializeAsync()
    {
        await _commands.InitializeAsync();
    }

    private void OnBTN1(object sender, EventArgs e)
    {
        _commands.TestChannelUpCommand();

    }
    private void OnBTN2(object sender, EventArgs e)
    {
        // await _commands.test2();
        _commands.TestVolumeCommand();
    }

      private async void SwitchDevice(object sender, EventArgs e)
    {
        _commands.CleanRemote();

        await Navigation.PopAsync();

        _enterCodePage.SwitchDevice();
    }


}