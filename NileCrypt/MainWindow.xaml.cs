using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NileCrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private bool tehfuckshouldinameit;

        public bool TheFuckShouldINameIt
        {
            get
            {
                return tehfuckshouldinameit;
            }
            set
            {
                tehfuckshouldinameit = value; OnPropertyChanged();
            }
        }

        private bool encryptSubDirectories;

        public bool EncryptSubDirectories
        {
            get
            {
                return encryptSubDirectories;
            }
            set
            {
                encryptSubDirectories = value; OnPropertyChanged();
            }
        }

        private bool canExecute;

        public bool CanExecute
        {
            get
            {
                return canExecute;
            }
            set
            {
                canExecute = value; OnPropertyChanged();
            }
        }

        private bool deleteInputFiles;

        public bool DeleteInputFiles
        {
            get
            {
                return deleteInputFiles;
            }
            set
            {
                deleteInputFiles = value; OnPropertyChanged();
            }
        }

        private bool deleteOutputFiles;

        public bool DeleteOutputFiles
        {
            get
            {
                return deleteOutputFiles;
            }
            set
            {
                deleteOutputFiles = value; OnPropertyChanged();
            }
        }


        private string inPath;

        public string InPath
        {
            get
            {
                return inPath;
            }
            set
            {
                inPath = value; OnPropertyChanged();
            }
        }

        private string outPath;

        public string OutPath
        {
            get
            {
                return outPath;
            }
            set
            {
                outPath = value; OnPropertyChanged();
            }
        }

        private string key;

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value; OnPropertyChanged();
            }
        }

        private OperationType opType;

        /// <summary>
        /// file or folder
        /// </summary>
        public OperationType OpType { get { return opType; } set { opType = value; OnPropertyChanged(); } }

        private OperationType2 opType2;

        /// <summary>
        /// whether encyrpt or decrypt
        /// </summary>
        public OperationType2 OpType2 { get { return opType2; } set { opType2 = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private SHA256Managed sha;

        private ObservableCollection<string> outputLogs;

        public MainWindow()
        {
            InitializeComponent();

            Key = "";
            InPath = "";
            OutPath = "";

            sha = new SHA256Managed();
            outputLogs = new ObservableCollection<string>();
            outputLb.ItemsSource = outputLogs;
            CanExecute = true;
        }

        private void Info(object o)
        {
            MessageBox.Show(o.ToString(), "NileCrypt", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void EncryptFile(string inPath, string outPath, byte[] key)
        {
            using (var stream = new FileStream(inPath, FileMode.Open, FileAccess.Read))
            {
                var hash = sha.ComputeHash(stream);
                stream.Position = 0;
                using (var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write))
                {
                    using (var aes = new AesCng())
                    {
                        aes.Key = key;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.GenerateIV();

                        int blockSizeBytes = aes.BlockSize / 8;

                        byte[] bytes = new byte[blockSizeBytes];

                        bool wroteTheHash = false;

                        outStream.Write(aes.IV, 0, aes.IV.Length);

                        using (var crypto = aes.CreateEncryptor())
                        {
                            using (var cryptoStream = new CryptoStream(outStream, crypto, CryptoStreamMode.Write))
                            {
                                while (stream.Position < stream.Length)
                                {
                                    int n = stream.Read(bytes, 0, blockSizeBytes);

                                    cryptoStream.Write(bytes, 0, n);

                                    if (!(n == blockSizeBytes))
                                    {
                                        cryptoStream.Write(hash, 0, hash.Length);
                                        wroteTheHash = true;
                                        cryptoStream.FlushFinalBlock();
                                    }
                                    else
                                    {
                                        cryptoStream.Flush();

                                    }
                                }

                                if (!wroteTheHash)
                                {
                                    cryptoStream.Write(hash, 0, hash.Length);
                                    wroteTheHash = true;
                                    cryptoStream.FlushFinalBlock();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DecryptFile(string inPath, string outPath, byte[] key)
        {
            bool success = false;

            using (var stream = new FileStream(inPath, FileMode.Open, FileAccess.Read))
            {
                using (var outStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var aes = new AesCng())
                    {
                        aes.Key = key;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.GenerateIV();
                        byte[] iv = new byte[aes.IV.Length];
                        stream.Read(iv, 0, aes.IV.Length);
                        aes.IV = iv;
                        using (var crypto = aes.CreateDecryptor())
                        {
                            using (var cryptoStream = new CryptoStream(outStream, crypto, CryptoStreamMode.Write))
                            {
                                int hashSize = sha.HashSize / 8;

                                int blockSizeBytes = aes.BlockSize / 8;

                                byte[] bytes = new byte[blockSizeBytes];

                                while (stream.Position < stream.Length)
                                {
                                    int n = stream.Read(bytes, 0, blockSizeBytes);

                                    cryptoStream.Write(bytes, 0, n);

                                    if (!(n == blockSizeBytes))
                                    {
                                        cryptoStream.FlushFinalBlock();
                                    }
                                    else
                                    {
                                        cryptoStream.Flush();
                                    }
                                }

                                cryptoStream.FlushFinalBlock();

                                byte[] hash1 = new byte[hashSize];

                                long positionOfHashStart = outStream.Length - hashSize;

                                outStream.Position = positionOfHashStart;
                                outStream.Read(hash1, 0, hashSize);

                                outStream.Position = 0;

                                outStream.SetLength(outStream.Length - hashSize);

                                outStream.Flush();

                                outStream.Position = 0;

                                byte[] hash2 = sha.ComputeHash(outStream);
                                success = hash1.SequenceEqual(hash2);
                            }
                        }
                    }
                }
            }

            if (!success)
                throw new Exception("The stored decrypted hash and the computed hash do not match.");
        }

        private void EncryptFolder(string inPath, string outPath, byte[] key, bool doSubDirs, bool deleteFailiures, bool deleteOriginal)
        {
            SearchOption option = doSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            inPath = Path.GetFullPath(inPath);

            outPath = Path.GetFullPath(outPath);

            var files = Directory.GetFiles(inPath, "*", option);

            float valuePerFile = 100f / files.Length;

            bool allEncrypted = true;

            bool allDeleted = true;

            foreach (var inFile in files)
            {
                var outFolder = outPath + "\\" + Path.GetDirectoryName(inFile.Substring(inPath.Length));
                var outputFile = Path.Combine(outFolder, Path.GetFileName(inFile) + ".nilecrypt");
                bool success = true;

                try
                {
                    Directory.CreateDirectory(outFolder);
                    EncryptFile(inFile, outputFile, key);
                    mainWnd.Dispatcher.Invoke(() =>
                        {
                            outputLogs.Add("Encrypted file " + inFile + ".");
                        });
                }
                catch (Exception e)
                {
                    allEncrypted = false;
                    success = false;
                    mainWnd.Dispatcher.Invoke(() =>
                        {
                            outputLogs.Add("Failed encryption of file " + inFile + ". Reason: " + e.Message);
                        });

                    if (deleteFailiures)
                    {
                        try
                        {
                            if (File.Exists(outputFile))
                                File.Delete(outputFile);
                        }
                        catch (Exception ex)
                        {
                            mainWnd.Dispatcher.Invoke(() =>
                            {
                                outputLogs.Add("Could not delete failed file " + inFile + ". Reason: " + ex.Message);
                            });
                            allDeleted = false;
                        }
                    }
                }

                if (deleteOriginal && success)
                {
                    try
                    {
                        File.Delete(inFile);
                    }
                    catch (Exception ez)
                    {
                        mainWnd.Dispatcher.Invoke(() =>
                        {
                            outputLogs.Add("Could not delete original file " + inFile + ". Reason: " + ez.Message);
                        });
                        allDeleted = false;
                    }
                }

                mainWnd.Dispatcher.Invoke(() =>
                {
                    progressBar.Value += valuePerFile;
                });
            }

            if(allDeleted && DeleteInputFiles)
            {
                try
                {
                    Directory.Delete(inPath);
                }
                catch(Exception ev)
                {
                    mainWnd.Dispatcher.Invoke(() =>
                    {
                        outputLogs.Add("All files that were marked for deletion were deleted, but the folder wasn't. Reason: " + ev.Message);
                    });
                    allDeleted = false;
                }
            }

            mainWnd.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 100;
                outputLogs.Add("Finished operation.");
                if(allEncrypted)
                    outputLogs.Add("All files that were marked for encryption were encrypted.");
                if (allDeleted)
                    outputLogs.Add("All files that were marked for deletion were deleted.");
            });
        }

        private void DecryptFolder(string inPath, string outPath, byte[] key, bool doSubDirs, bool deleteFailiures, bool deleteOriginal)
        {
            SearchOption option = doSubDirs ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            inPath = Path.GetFullPath(inPath);

            outPath = Path.GetFullPath(outPath);

            var files = Directory.GetFiles(inPath, "*", option);

            float valuePerFile = 100f / files.Length;

            bool allDecrypted = true;

            bool allDeleted = true;

            foreach (var inFile in files)
            {
                var outFolder = outPath + "\\" + Path.GetDirectoryName(inFile.Substring(inPath.Length));
                string filename;
                if (Path.GetExtension(inFile) == ".nilecrypt")
                    filename = Path.GetFileNameWithoutExtension(inFile);
                else
                    filename = Path.GetFileName(inFile);
                var outputFile = Path.Combine(outFolder, filename);
                bool success = true;

                try
                {
                    Directory.CreateDirectory(outFolder);
                    DecryptFile(inFile, outputFile, key);
                    mainWnd.Dispatcher.Invoke(() =>
                    {
                        outputLogs.Add("Decrypted file " + inFile + ".");
                    });
                }
                catch (Exception e)
                {
                    allDecrypted = false;
                    success = false;
                    mainWnd.Dispatcher.Invoke(() =>
                    {
                        outputLogs.Add("Failed decryption of file " + inFile + ". Reason: " + e.Message);
                    });

                    if (deleteFailiures)
                    {
                        try
                        {
                            if (File.Exists(outputFile))
                                File.Delete(outputFile);
                        }
                        catch (Exception ex)
                        {
                            mainWnd.Dispatcher.Invoke(() =>
                            {
                                outputLogs.Add("Could not delete failed file " + inFile + ". Reason: " + ex.Message);
                            });
                            allDeleted = false;
                        }
                    }

                    if (deleteOriginal && success)
                    {
                        try
                        {
                            File.Delete(inFile);
                        }
                        catch (Exception ez)
                        {
                            mainWnd.Dispatcher.Invoke(() =>
                            {
                                outputLogs.Add("Could not delete original file " + inFile + ". Reason: " + ez.Message);
                            });
                            allDeleted = false;
                        }
                    }
                }

                mainWnd.Dispatcher.Invoke(() =>
                {
                    progressBar.Value += valuePerFile;
                });
            }

            if (allDeleted && DeleteInputFiles)
            {
                try
                {
                    Directory.Delete(inPath);
                }
                catch (Exception ev)
                {
                    mainWnd.Dispatcher.Invoke(() =>
                    {
                        outputLogs.Add("All files that were marked for deletion were deleted, but the folder wasn't. Reason: " + ev.Message);
                    });
                    allDeleted = false;
                }
            }

            mainWnd.Dispatcher.Invoke(() =>
            {
                progressBar.Value = 100;
                outputLogs.Add("Finished operation.");
                if (allDecrypted)
                    outputLogs.Add("All files that were marked for decryption were decrypted.");
                if (allDeleted)
                    outputLogs.Add("All files that were marked for deletion were deleted.");
            });
        }

        private string GetPathFromNileCrypt(string npath)
        {
            return Path.GetFileNameWithoutExtension(npath);
        }

        //browse output tbn 
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string path = "";

            switch (OpType)
            {
                case OperationType.File:
                    var dialog = new SaveFileDialog();
                    dialog.Title = "NileCrypt file output path selector";
                    if (OpType2 == OperationType2.Encrypt)
                    {
                        dialog.FileName = InPath + ".nilecrypt";
                    }
                    else
                    {
                        if (InPath.Length > 10 && InPath.Substring(InPath.Length - 10, 10) == ".nilecrypt")
                        {
                            dialog.FileName = GetPathFromNileCrypt(InPath);
                        }
                    }
                    if (dialog.ShowDialog() == true)
                    {
                        path = dialog.FileName;
                    }
                    break;
                case OperationType.Folder:
                    var dlg = new FolderBrowserDialog();
                    dlg.Description = "NileCrypt folder output path selector";
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        path = dlg.SelectedPath;
                    }
                    break;
                default:
                    throw new Exception("WHY THE FUCK NOT");
            }

            OutPath = path;
        }

        //browse intput tbn 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = "";

            switch (OpType)
            {
                case OperationType.File:
                    var dialog = new OpenFileDialog();
                    dialog.Title = "NileCrypt file output path selector";
                    if (dialog.ShowDialog() == true)
                    {
                        path = dialog.FileName;
                    }
                    break;
                case OperationType.Folder:
                    var dlg = new FolderBrowserDialog();
                    dlg.Description = "NileCrypt folder output path selector";
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        path = dlg.SelectedPath;
                    }
                    break;
                default:
                    throw new Exception("WHY THE FUCK NOT");
            }

            InPath = path;
        }

        //folder radio clicked
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            TheFuckShouldINameIt = OpType == OperationType.Folder;
        }

        //execurte operation click
        private async void Button_Click_1(object sender, RoutedEventArgs args)
        {
            if (MessageBox.Show(OpType2 + " " + OpType + "?", "Confirm NileCrypt", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                progressBar.Value = 0;
                outputLogs.Clear();
                CanExecute = false;
                var key = sha.ComputeHash(Encoding.UTF8.GetBytes(Key));

                switch (OpType)
                {
                    case OperationType.File:
                        switch (OpType2)
                        {
                            case OperationType2.Encrypt:
                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        EncryptFile(InPath, OutPath, key);
                                    });
                                    Info("Success.");
                                }
                                catch (Exception e)
                                {
                                    Info("Failed encrypting the file. Reason: " + e.Message);
                                }
                                break;
                            case OperationType2.Decrypt:
                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        DecryptFile(InPath, OutPath, key);
                                    });
                                    Info("Success.");
                                }
                                catch (Exception e)
                                {
                                    Info("Failed decrypting the file. Reason: " + e.Message);
                                }
                                break;
                            default:
                                throw new Exception("WHY THE FUCK NOT 2");
                        }
                        break;
                    case OperationType.Folder:

                        switch (OpType2)
                        {
                            case OperationType2.Encrypt:
                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        EncryptFolder(InPath, OutPath, key, EncryptSubDirectories, DeleteInputFiles, DeleteOutputFiles);
                                    });
                                    Info("Operation done.");
                                }
                                catch (Exception e)
                                {
                                    Info("Failed Decrypting the folder. Reason: " + e.Message);
                                }
                                break;
                            case OperationType2.Decrypt:
                                try
                                {
                                    await Task.Run(() =>
                                    {
                                        DecryptFolder(InPath, OutPath, key, EncryptSubDirectories, DeleteInputFiles, DeleteOutputFiles);
                                    });
                                    Info("Operation done.");

                                }
                                catch (Exception e)
                                {
                                    Info("Failed decrypting the folder. Reason: " + e.Message);
                                }
                                break;
                            default:
                                throw new Exception("WHY THE FUCK NOT 2");
                        }
                        break;
                    default:
                        throw new Exception("WHY THE FUCK NOT 3");
                }
            }

            CanExecute = true;
        }

        private void mainWnd_Closed(object sender, EventArgs e)
        {
            sha.Dispose();
        }
    }
}
