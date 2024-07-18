namespace RemoteForAndroidTV;
using System;
using Microsoft.Maui.Controls;

public partial class MainRemote : ContentPage{

    private bool isRemoteOn = false;
    RemoteButtons _remote;

    
    public MainRemote(RemoteButtons remote, RemoteState remoteStateInfo){

        InitializeComponent();

        _remote = remote;

        SubscribeToEvents();

        InitValues(remoteStateInfo);
    }

    protected override void OnAppearing()
    {
        SubscribeToOnResume();
    }
    protected override void OnDisappearing()
    {
        UnSubscribeOnResume();

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

    private void IncreaseVolume(object sender, EventArgs e)
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
        isRemoteOn = (isOnState == "1"); // if its 1 then its on

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

    private void onResume(){
        Console.WriteLine("resumed");
        _remote.OnResume();
    }

    private void SubscribeToOnResume(){
        Console.WriteLine("SUBSCRIBE RESUME");
         MessagingCenter.Subscribe<App>(this, "AppEnteredForeground", (sender) =>
            {
                onResume();
            });
    }

    private void UnSubscribeOnResume(){
        Console.WriteLine("UNUNUNSUBSCRIBE RESUME");
        MessagingCenter.Unsubscribe<App>(this, "AppEnteredForeground");
    }

}