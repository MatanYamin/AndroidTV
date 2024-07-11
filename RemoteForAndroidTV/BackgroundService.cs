using System.Threading;
using System.Threading.Tasks;

public class BackgroundService : IDisposable
{
    private Timer? _timer;
    private readonly IValues _values;

    public BackgroundService(IValues values)
    {
        _values = values;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        if (!_values.IsConnectedToInternet())
        {
            // Show an alert or handle no internet connection
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert("No Internet", "You are not connected to the internet", "OK");
            });
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
