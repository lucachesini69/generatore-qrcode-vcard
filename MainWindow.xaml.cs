using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;
using Microsoft.Win32;
using System.IO;

namespace VCardQRGenerator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BitmapImage? currentQRCode;

    public MainWindow()
    {
        InitializeComponent();
        SaveButton.IsEnabled = false;
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Validazione campi obbligatori
            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Nome e Cognome sono campi obbligatori!",
                    "Errore Validazione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Genera il contenuto vCard
            string vCardContent = GenerateVCard();
            VCardTextBox.Text = vCardContent;

            // Genera il QR Code
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(vCardContent, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);

            // Converti in BitmapImage
            currentQRCode = new BitmapImage();
            using (var stream = new MemoryStream(qrCodeBytes))
            {
                currentQRCode.BeginInit();
                currentQRCode.CacheOption = BitmapCacheOption.OnLoad;
                currentQRCode.StreamSource = stream;
                currentQRCode.EndInit();
            }

            QRCodeImage.Source = currentQRCode;
            SaveButton.IsEnabled = true;
            StatusTextBlock.Text = "QR Code generato con successo!";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante la generazione del QR Code: {ex.Message}",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Errore nella generazione";
        }
    }

    private string GenerateVCard()
    {
        StringBuilder vCard = new StringBuilder();

        vCard.AppendLine("BEGIN:VCARD");
        vCard.AppendLine("VERSION:3.0");

        // Nome e cognome
        vCard.AppendLine($"N:{LastNameTextBox.Text};{FirstNameTextBox.Text};;;");
        vCard.AppendLine($"FN:{FirstNameTextBox.Text} {LastNameTextBox.Text}");

        // Organizzazione e titolo
        if (!string.IsNullOrWhiteSpace(OrganizationTextBox.Text))
            vCard.AppendLine($"ORG:{OrganizationTextBox.Text}");

        if (!string.IsNullOrWhiteSpace(TitleTextBox.Text))
            vCard.AppendLine($"TITLE:{TitleTextBox.Text}");

        // Email
        if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            vCard.AppendLine($"EMAIL:{EmailTextBox.Text}");

        // Telefoni
        if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            vCard.AppendLine($"TEL;TYPE=WORK,VOICE:{PhoneTextBox.Text}");

        if (!string.IsNullOrWhiteSpace(MobileTextBox.Text))
            vCard.AppendLine($"TEL;TYPE=CELL:{MobileTextBox.Text}");

        // Indirizzo
        if (!string.IsNullOrWhiteSpace(AddressTextBox.Text) ||
            !string.IsNullOrWhiteSpace(CityTextBox.Text) ||
            !string.IsNullOrWhiteSpace(StateTextBox.Text) ||
            !string.IsNullOrWhiteSpace(ZipCodeTextBox.Text) ||
            !string.IsNullOrWhiteSpace(CountryTextBox.Text))
        {
            vCard.AppendLine($"ADR;TYPE=WORK:;;{AddressTextBox.Text};{CityTextBox.Text};{StateTextBox.Text};{ZipCodeTextBox.Text};{CountryTextBox.Text}");
        }

        // Sito web
        if (!string.IsNullOrWhiteSpace(WebsiteTextBox.Text))
            vCard.AppendLine($"URL:{WebsiteTextBox.Text}");

        // Note
        if (!string.IsNullOrWhiteSpace(NotesTextBox.Text))
        {
            // Le note possono contenere caratteri speciali, quindi le escapiamo
            string escapedNotes = NotesTextBox.Text
                .Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace(",", "\\,")
                .Replace(";", "\\;");
            vCard.AppendLine($"NOTE:{escapedNotes}");
        }

        vCard.AppendLine("END:VCARD");

        return vCard.ToString();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (currentQRCode == null)
            {
                MessageBox.Show("Nessun QR Code da salvare!",
                    "Avviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp",
                Title = "Salva QR Code",
                FileName = $"QRCode_{FirstNameTextBox.Text}_{LastNameTextBox.Text}.png"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                BitmapEncoder encoder;

                switch (Path.GetExtension(saveFileDialog.FileName).ToLower())
                {
                    case ".jpg":
                    case ".jpeg":
                        encoder = new JpegBitmapEncoder();
                        break;
                    case ".bmp":
                        encoder = new BmpBitmapEncoder();
                        break;
                    default:
                        encoder = new PngBitmapEncoder();
                        break;
                }

                encoder.Frames.Add(BitmapFrame.Create(currentQRCode));

                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fileStream);
                }

                StatusTextBlock.Text = "QR Code salvato con successo!";
                MessageBox.Show("QR Code salvato con successo!",
                    "Successo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Errore durante il salvataggio: {ex.Message}",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusTextBlock.Text = "Errore nel salvataggio";
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        FirstNameTextBox.Clear();
        LastNameTextBox.Clear();
        OrganizationTextBox.Clear();
        TitleTextBox.Clear();
        EmailTextBox.Clear();
        PhoneTextBox.Clear();
        MobileTextBox.Clear();
        AddressTextBox.Clear();
        CityTextBox.Clear();
        ZipCodeTextBox.Clear();
        StateTextBox.Clear();
        CountryTextBox.Clear();
        WebsiteTextBox.Clear();
        NotesTextBox.Clear();

        VCardTextBox.Clear();
        QRCodeImage.Source = null;
        currentQRCode = null;
        SaveButton.IsEnabled = false;
        StatusTextBlock.Text = "Pronto";
    }
}