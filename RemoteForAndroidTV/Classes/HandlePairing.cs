
using RemoteForAndroidTV;

public class HandlePairing
{
    PairAndConnect _pairAndConnectHandler;
    Pairing _pairing;
    private readonly string ip;
    IValues PLATFORM_VALUES;

    public HandlePairing(PairAndConnect pac)
    {

        this._pairAndConnectHandler = pac;
        this.ip = pac.ip;

        this._pairing = new Pairing(this.ip, this);

        PLATFORM_VALUES = PlatformManager.GetPlatformValues();

        StartPairing();
    }

    private async void StartPairing(){
        // PLATFORM_VALUES.ShowKeyboard();
        await _pairing.StartPairing();

    }

    public void HandleOnEntryTextChanged(object sender, TextChangedEventArgs e){

        var entry = sender as Entry;
        if (entry == null) return;

        // Filter out any non-letter and non-digit characters
        if (!string.IsNullOrEmpty(entry.Text))
        {
            string newText = new string(entry.Text.Where(char.IsLetterOrDigit).ToArray()).ToUpper();

            // Enforce max length of 6 characters
            if (newText.Length > 6)
            {
                newText = newText.Substring(0, 6);
            }

            // Update the text if it has changed
            if (entry.Text != newText)
            {
                entry.Text = newText;
            }
       
        }
    }

    public async void HandleOnOkButtonClicked(object sender, EventArgs e){

        int codeLen = _pairAndConnectHandler.GetTVcodeCount();

        if(codeLen != 6){
            // TODO: Add a line that say not entered 6 letters
            return;
        }

        // Concatenate the text from all entry fields
        string enteredCode = _pairAndConnectHandler.GetTvCodeString();

        bool connectedSuccess = await ConnectWithCode(enteredCode);

        if(connectedSuccess){
            _pairAndConnectHandler.StartConnecting();
        }
        else{

            _pairAndConnectHandler.ConnectionFailed();
        }

    }

    async Task<bool> ConnectWithCode(string tvCode){

        return await _pairing.ConnectWithCode(tvCode);

    }

    public void ConnectionFailed(){

        _pairAndConnectHandler.ConnectionFailed();

    }

}