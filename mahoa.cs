using System;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace btlatbmtt
{
    public partial class Mahoapanel : UserControl
    {
        private string chuoiketnoi = "Data Source=VU\\SQLEXPRESS;Initial Catalog=atbm;Integrated Security=True;"; // Initialize your connection string
        private string selectedFilePath; // Store the full file path


        public Mahoapanel()
        {
            InitializeComponent();
            load_ketqua.Visible = true; // Ẩn ProgressBar khi khởi động
        }

        private void Mahoa_Load(object sender, EventArgs e)
        {

        }

        private void btn_them_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Chọn file để mã hóa hoặc giải mã";
                openFileDialog.Filter = "All files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = openFileDialog.FileName; // Store the full file path
                    txt_file.Text = Path.GetFileName(selectedFilePath); // Display only the file name
                }
            }
        }

        private async void btn_mahoa_Click(object sender, EventArgs e)
        {
            load_ketqua.Visible = true;
            load_ketqua.Value = 0;
            txt_ketqua.Clear();

            try
            {
                if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    txt_ketqua.Text = "File không tồn tại.";
                    return;
                }

                string password = GenerateRandomPassword(16);
                string encryptedFilePath = selectedFilePath + ".enc";

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(password.PadRight(32).Substring(0, 32));
                    aes.IV = new byte[16];

                    using (FileStream fsInput = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read))
                    using (FileStream fsOutput = new FileStream(encryptedFilePath, FileMode.Create, FileAccess.Write))
                    using (CryptoStream cs = new CryptoStream(fsOutput, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytes = fsInput.Length;
                        long bytesWritten = 0;

                        while ((bytesRead = await fsInput.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await cs.WriteAsync(buffer, 0, bytesRead);
                            bytesWritten += bytesRead;

                            int percentComplete = (int)((double)bytesWritten / totalBytes * 100);
                            load_ketqua.Value = percentComplete;

                            Application.DoEvents(); // Keep UI responsive
                        }
                    }
                }

                txt_kbm.Text = password;
                load_ketqua.Value = 100;
                MessageBox.Show("Mã hóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txt_ketqua.Text = "File đã được mã hóa thành công: " + encryptedFilePath;

                // Save encryption info to the database
                SaveEncryptionInfoToDatabase(Path.GetFileName(selectedFilePath), password, DateTime.Now);
            }
            catch (Exception ex)
            {
                txt_ketqua.Text = "Lỗi mã hóa: " + ex.Message;
            }
            finally
            {
                load_ketqua.Visible = false;
            }
        }

        private void SaveEncryptionInfoToDatabase(string tenfile, string khoabaomat, DateTime ngaygio)
        {
            string query = "INSERT INTO lich_su (tenfile, khoabaomat, ngaygio) VALUES (@tenfile, @khoabaomat, @ngaygio)";

            using (SqlConnection connection = new SqlConnection(chuoiketnoi))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tenfile", tenfile);
                    command.Parameters.AddWithValue("@khoabaomat", khoabaomat);
                    command.Parameters.AddWithValue("@ngaygio", ngaygio);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        MessageBox.Show("Thông tin mã hóa đã được lưu vào cơ sở dữ liệu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi lưu thông tin vào cơ sở dữ liệu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private string GenerateRandomPassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        

    }

    private async void btn_giaima_Click(object sender, EventArgs e)
        {
            load_ketqua.Visible = true;
            load_ketqua.Value = 0;
            txt_ketqua.Clear();

            try
            {
                if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
                {
                    txt_ketqua.Text = "File không tồn tại.";
                    return;
                }

                string password = txt_kbm.Text;
                string decryptedFilePath = selectedFilePath.Replace(".enc", ""); // Remove .enc extension

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(password.PadRight(32).Substring(0, 32));
                    aes.IV = new byte[16];

                    using (FileStream fsInput = new FileStream(selectedFilePath, FileMode.Open, FileAccess.Read))
                    using (FileStream fsOutput = new FileStream(decryptedFilePath, FileMode.Create, FileAccess.Write))
                    using (CryptoStream cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        long totalBytes = fsInput.Length;
                        long bytesWritten = 0;

                        while ((bytesRead = await cs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fsOutput.WriteAsync(buffer, 0, bytesRead);
                            bytesWritten += bytesRead;

                            int percentComplete = (int)((double)bytesWritten / totalBytes * 100);
                            load_ketqua.Value = percentComplete;

                            Application.DoEvents(); // Keep UI responsive
                        }
                    }
                }

                load_ketqua.Value = 100;
                MessageBox.Show("Giải mã thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txt_ketqua.Text = "File đã được giải mã thành công: " + decryptedFilePath;
            }
            catch (Exception ex)
            {
                txt_ketqua.Text = "Lỗi giải mã: " + ex.Message;
            }
            finally
            {
                load_ketqua.Visible = false;
            }
        }

        private void btn_sc_Click(object sender, EventArgs e)
        {
            // Sao chép khóa bảo mật vào clipboard
            Clipboard.SetText(txt_kbm.Text);
            MessageBox.Show("Đã sao chép.", "Thông báo");
        }

        private void btn_xoa_Click(object sender, EventArgs e)
        {
            // Xóa khóa bảo mật
            txt_kbm.Clear();
            MessageBox.Show("Khóa bảo mật đã được xóa.", "Thông báo");
        }

        private void btn_reset_Click(object sender, EventArgs e)
        {
            // Clear all textboxes
            txt_file.Clear();
            txt_kbm.Clear();
            txt_ketqua.Clear();

            // Reset ProgressBar
            load_ketqua.Value = 0;
            load_ketqua.Visible = false;

            // Refresh the form
            this.Refresh();

            MessageBox.Show("Làm mới thành công.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
