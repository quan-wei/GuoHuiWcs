using Guohui_Wcs.Services;
using Microsoft.Extensions.Logging;

namespace Guohui_Test;

public partial class Form2 : Form
{
    private readonly ApiClientService _apiClient;
    private readonly ILoggerFactory _loggerFactory;

    public Form2()
    {
        InitializeComponent();
        _loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = _loggerFactory.CreateLogger<ApiClientService>();
        _apiClient = new ApiClientService(logger);
    }

    private async void button1_ClickAsync(object sender, EventArgs e)
    {
        var barcode = await _apiClient.SyncBardossierToDbAsync("F0.08.014950085160");
        textBox2.Text = barcode?.ToString() ?? "Barcode not found";
    }

    private void button2_Click(object sender, EventArgs e)
    {
        Form1 form1= new Form1();
        form1.ShowDialog();
    }
}