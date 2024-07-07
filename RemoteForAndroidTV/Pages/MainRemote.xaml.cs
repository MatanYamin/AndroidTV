namespace RemoteForAndroidTV;
using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Reflection;
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