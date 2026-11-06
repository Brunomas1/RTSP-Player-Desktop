using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using LibVLCSharp.Shared;

namespace CameraViewer;

public partial class MainWindow : Window
{
    private LibVLC? _libVlc;
    private MediaPlayer? _player;
    private readonly string _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CameraViewer", "connection.json");
    public MainWindow()
    {
        InitializeComponent();
        try
        {
            Core.Initialize();
            _libVlc = new LibVLC("--no-video-title-show", "--network-caching=1000", "--avcodec-hw=any");
            _player = new MediaPlayer(_libVlc);
            Video.MediaPlayer = _player;
            _player.EncounteredError += (_, _) => Dispatcher.Invoke(() => { StatusText.Text = "Não foi possível abrir o stream. Verifique o endereço e a rede."; ShowSetup(); });
            _player.Playing += (_, _) => Dispatcher.BeginInvoke(new Action(async () =>
            {
                StatusText.Text = "Ao vivo";
                await ShowVideo();
            }));
            _player.Buffering += (_, e) => Dispatcher.Invoke(() => StatusText.Text = $"Carregando vídeo: {e.Cache:0}%");
            LoadSaved();
        }
        catch (Exception ex) { StatusText.Text = "Falha ao iniciar o decodificador: " + ex.Message; }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Pede ao DWM a barra de título escura, mantendo a moldura nativa (minimizar/maximizar/fechar).
        ApplyDarkTitleBar(new WindowInteropHelper(this).Handle);
    }

    private static void ApplyDarkTitleBar(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return;
        var enabled = 1;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 20H1+ / Windows 11).
        // 19 = mesmo atributo em builds anteriores do Windows 10.
        if (DwmSetWindowAttribute(hwnd, 20, ref enabled, sizeof(int)) != 0)
            DwmSetWindowAttribute(hwnd, 19, ref enabled, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void LoadSaved()
    {
        if (!File.Exists(_settingsPath)) return;
        try
        {
            var data = JsonSerializer.Deserialize<ConnectionData>(File.ReadAllText(_settingsPath));
            if (data is null) return;
            var link = data.Link is null
                ? null
                : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(data.Link), null, DataProtectionScope.CurrentUser));
            if (string.IsNullOrWhiteSpace(link) && !string.IsNullOrWhiteSpace(data.Url))
            {
                link = data.Url;
                if (!string.IsNullOrEmpty(data.Password))
                {
                    var oldPassword = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(data.Password), null, DataProtectionScope.CurrentUser));
                    var legacyUri = new UriBuilder(link) { UserName = data.User ?? "", Password = oldPassword };
                    link = legacyUri.Uri.AbsoluteUri;
                }
            }
            if (string.IsNullOrWhiteSpace(link)) return;
            UrlBox.Text = link;
            _ = PlayStream(link);
        }
        catch { StatusText.Text = "Não foi possível ler a conexão salva. Configure novamente."; ShowSetup(); }
    }

    private void SaveConnect_Click(object sender, RoutedEventArgs e)
    {
        FormError.Text = "";
        var url = UrlBox.Text.Trim().Replace("\r", "").Replace("\n", "");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "rtsp") { FormError.Text = "Link RTSP inválido."; return; }
        try
        {
            if (RememberBox.IsChecked == true)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
                var encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(url), null, DataProtectionScope.CurrentUser);
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new ConnectionData(Link: Convert.ToBase64String(encrypted))));
            }
            else if (File.Exists(_settingsPath)) File.Delete(_settingsPath);
            _ = PlayStream(url);
        }
        catch (Exception ex) { FormError.Text = "Não foi possível salvar a conexão: " + ex.Message; }
    }

    private async Task PlayStream(string url)
    {
        if (_player is null || _libVlc is null) { StatusText.Text = "Decodificador indisponível."; return; }
        ShowConnecting();
        StatusText.Text = "Conectando à câmera...";
        _player.Stop();
        await Dispatcher.InvokeAsync(() => Video.UpdateLayout(), System.Windows.Threading.DispatcherPriority.Loaded);
        await Task.Delay(100);
        using var media = new Media(_libVlc, new Uri(url));
        media.AddOption(":rtsp-tcp");
        media.AddOption(":network-caching=1000");
        media.AddOption(":avcodec-hw=any");
        _player.Play(media);
    }

    private void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UrlBox.Text)) { ShowSetup(); return; }
        _ = PlayStream(UrlBox.Text.Trim());
    }
    private void Edit_Click(object sender, RoutedEventArgs e) => ShowSetup();

    // O formulário e o vídeo não se sobrepõem, pois VideoView hospeda uma janela nativa.
    private void ShowSetup()
    {
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        MinWidth = 680;
        MinHeight = 440;
        VideoFrame.Width = double.NaN;
        VideoFrame.Height = double.NaN;
        HeaderBar.Visibility = Visibility.Visible;
        FooterBar.Visibility = Visibility.Visible;
        HeaderRow.Height = new GridLength(58);
        FooterRow.Height = new GridLength(38);
        ContentArea.Margin = new Thickness(16);
        VideoFrame.BorderThickness = new Thickness(0);
        Video.Visibility = Visibility.Collapsed;
        SetupPanel.Visibility = Visibility.Visible;
    }

    private Task ShowVideo()
    {
        SetupPanel.Visibility = Visibility.Collapsed;
        Video.Visibility = Visibility.Visible;
        return Task.CompletedTask;
    }

    private void ShowConnecting()
    {
        SetupPanel.Visibility = Visibility.Collapsed;
        // LibVLC precisa do HWND da VideoView antes de Play, senão cria uma janela VLC separada.
        Video.Visibility = Visibility.Visible;
    }


    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && SetupPanel.Visibility != Visibility.Visible)
        {
            _player?.Stop();
            ShowSetup();
            e.Handled = true;
        }
    }
    protected override void OnClosed(EventArgs e) { _player?.Stop(); _player?.Dispose(); _libVlc?.Dispose(); base.OnClosed(e); }
    private sealed record ConnectionData(string? Link, string? Url = null, string? User = null, string? Password = null);
}
