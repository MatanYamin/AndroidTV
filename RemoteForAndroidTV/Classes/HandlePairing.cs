
using RemoteForAndroidTV;

public class HandlePairing
{
    PairAndConnect _pairAndConnectHandler;
    Pairing _pairing;
    private readonly string ip;

    public HandlePairing(PairAndConnect pac)
    {

        this._pairAndConnectHandler = pac;
        this.ip = pac.ip;

        this._pairing = new Pairing(this.ip, this);

        StartPairing();
    }

    private async void StartPairing(){
        await _pairing.StartPairing();
    }

    public void HandleOnEntryTextChanged(object sender, TextChangedEventArgs e){

        var entry = sender as Entry;

        // Filter out any non-letter and non-digit characters
        if (!string.IsNullOrEmpty(entry.Text))
        {
        
            string newText = new string(entry.Text.Where(char.IsLetterOrDigit).ToArray()).ToUpper();

            if (entry.Text != newText)
            {
                entry.Text = newText;
            }
        }
        if (entry.Text.Length == 1)
        {
            _pairAndConnectHandler.FocusNextEntry(entry);
        }

        else if (string.IsNullOrEmpty(entry.Text))
        {
            _pairAndConnectHandler.FocusPreviousEntry(entry);
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