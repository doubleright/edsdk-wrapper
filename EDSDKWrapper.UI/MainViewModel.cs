using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EDSDKWrapper.Framework.Managers;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EDSDKWrapper.Framework.Objects;
using System.IO;

namespace EDSDKWrapper.UI
{
    public class MainViewModel : DependencyObject, IDisposable
    {
        #region Properties

        TaskScheduler UITaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        public FrameworkManager FrameworkManager { get; set; }
        public Task LiveViewCapturingTask { get; set; }
        public Camera Camera { get; set; }

        #region ImageSource

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MainViewModel));

        #endregion

        #endregion

        #region Methods

        #region Instance

        public MainViewModel()
        {
            this.FrameworkManager = new FrameworkManager();

            try
            {
                this.FrameworkManager.Dispose();
                this.FrameworkManager = new FrameworkManager();
            }
            catch 
            {
            }
        }

        #endregion

        #endregion

        #region Commands

        #region StartCapturingCommand

        public ICommand StartCapturingCommand { get { return new RelayCommand(StartCapturingCommand_Executed, StartCapturingCommand_CanExecute); } }

        private bool StartCapturingCommand_CanExecute()
        {
            return true;
        }

        private void StartCapturingCommand_Executed()
        {
        

            this.LiveViewCapturingTask = Task.Factory.StartNew(() =>
                {

                    this.Camera = this.FrameworkManager.Cameras.First();
                    this.Camera.StartLiveView();

                    while (Camera.LiveViewEnabled)
                    {
                        int exceptionCount = 0;
                        try
                        {
                            var stream = this.Camera.GetLiveViewImage();

                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = stream;
                            bitmapImage.EndInit();
                            bitmapImage.Freeze();

                            Dispatcher.BeginInvoke(new Action(() => 
                            {
                                this.ImageSource = bitmapImage;
                            }));

                            exceptionCount = 0;

                            Thread.Sleep(50);
                        }
                        catch(Exception ex)
                        {
                            Thread.Sleep(100);
                            if (++exceptionCount > 10)
                            {
                                throw;
                            }
                        }
                    }

                    Console.Write("");

                }).ContinueWith((previewsTask) => 
                {
                    if (previewsTask.IsFaulted)
                    {
                        throw previewsTask.Exception;                        
                    }
                },
                UITaskScheduler);


        }

        #endregion

        #region StopCapturingCommand

        public ICommand StopCapturingCommand { get { return new RelayCommand(StopCapturingCommand_Executed, StopCapturingCommand_CanExecute); } }

        private bool StopCapturingCommand_CanExecute()
        {
            return true;
        }

        private void StopCapturingCommand_Executed()
        {
            this.Camera.StopLiveView();

            this.Camera.Dispose();
            // this.LiveViewCapturingTask.Wait();
        }

        #endregion

        #region TakephotoCommand

        public ICommand TakephotoCommand { get { return new RelayCommand(TakephotoCommand_Executed, TakephotoCommand_CanExecute); } }

        private bool TakephotoCommand_CanExecute()
        {
            return true;
        }

        private void TakephotoCommand_Executed()
        {
            Camera.SaveTo = Framework.Enums.SaveTo.Host;
            Camera.ImageSaveDirectory = Path.Combine(Environment.CurrentDirectory, "Images");

            if (!Directory.Exists(Camera.ImageSaveDirectory))
            {
                Directory.CreateDirectory(Camera.ImageSaveDirectory);
            }

            this.Camera.TakePhoto();
        }

        #endregion
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            FrameworkManager.Dispose();
        }

        #endregion
    }
}
