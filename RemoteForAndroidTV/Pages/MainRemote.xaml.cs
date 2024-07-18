namespace RemoteForAndroidTV;
using System;
using Microsoft.Maui.Controls;
using RemoteForAndroidTV;

public partial class MainRemote : ContentPage{

    private bool isRemoteOn = false;
    RemoteButtons _remote;

    
    public MainRemote(RemoteButtons remote, RemoteState remoteStateInfo){

        InitializeComponent();

        _remote = remote;

        SubscribeToEvents();

        InitValues(remoteStateInfo);
    }

    void InitValues(RemoteState remoteStateInfo){

        OnVolumeChanged(remoteStateInfo.VolumeLevel);
        OnIsOnChanged(remoteStateInfo.IsOn);

        ReconnectButton.IsVisible = false;
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
        UnSubscribeToEvents();

        _remote.CleanRemote();

        await Navigation.PopToRootAsync();
    }

    private async void IncreaseVolume(object sender, EventArgs e)
    {
        _remote.IncreaseVolume();
    }

   private void OnVolumeChanged(string volumeLevel)
    {
        if(!isRemoteOn){return;}
        ChangeVol(volumeLevel);
    }

    private void OnIsOnChanged(string isOnState)
    {
        isRemoteOn = (isOnState == "1"); // if is 1 then its on

        if(!isRemoteOn){
            ChangeVol("");
        }

        ChangeIsOn(isOnState);

    }

    void ChangeVol(string value){

        VolumeLevel.Text = value;
        //TODO:: ADD RELEVANT UI CHANGE
        // FOR EXAMPLE - CHANGE THE COLOR OF THE BUTTON ACCORDINGLY

    }

    void ChangeIsOn(string value){

        
        IsOn.Text = value.ToString();
        //TODO:: ADD RELEVANT UI CHANGE
        // FOR EXAMPLE - CHANGE THE TURN OF BUTTON TO RED/GREEN
    }

    void SubscribeToEvents(){
        RemoteConnection.VolumeChangedEvent += OnVolumeChanged;
        RemoteConnection.IsOnChangedEvent += OnIsOnChanged;
    }

    void UnSubscribeToEvents(){

        RemoteConnection.VolumeChangedEvent -= OnVolumeChanged;
        RemoteConnection.IsOnChangedEvent -= OnIsOnChanged;
    }

    public async void ConnectionFailedShowReConnectButton(){
        ReconnectButton.IsVisible = true;
        // await Navigation.PopAsync();
    }

    public async void OnReconnect(object sender, EventArgs e){

        await Navigation.PopAsync();

    }

}