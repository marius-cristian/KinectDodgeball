using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.Threading;
using System.IO;



namespace KinectWpfDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer backgroundMusic = new MediaPlayer();
        private KinectSensor sensor;
        private Shape currentShape;
        private DepthImagePixel[] depthImagePixels;
        private int recognizeCount=0;
        private ColorImagePoint lastPoint;
        private Shape lastMarker;
        private RecognizerInfo recognizer;
        private SpeechRecognitionEngine speechRecognitionEngine;
        private int spectre=1;
        private int score = 0;
        private int r, g, b;        
        private Point leftHandPoint=new Point();
        private Point leftFootPoint = new Point();
        private Point rightHandPoint=new Point();
        private Point rightFootPoint = new Point();
        private BitmapImage[] images;
        private System.Media.SoundPlayer[] triggers;
        private int level;
        private Image boss = new Image();
        private Image projectile_1 = new Image();
        private Image projectile_2 = new Image();
        private Image projectile_3 = new Image();
        private List<Image> projectiles = new List<Image>();
        private int direction = 1;
        double bossX = 450, bossY = 140;
        double projectileX, projectileY;
        double relativeProjX, relativeProjY;
        double speed = 5;
        bool inLevel = false;
        bool projStatus = false;
        bool levelEnd = true;
        bool isDrawing = false;
        bool isSet = true;
        

        public MainWindow()
        {
            InitializeComponent();
            LoadResources();
        }

        public void LoadResources()
        {

            string path = "E:\\Projects\\VisualStudio\\KinectWpfDemo\\KinectWpfDemo\\Sprites\\";
            string[] files = Directory.GetFiles(path, "*.png");
            images = new BitmapImage[files.Count()];
            int i = 0;
            foreach (var file in files)
            {
                Uri aux = new Uri(file);
                images[i] = new BitmapImage(aux);
                i++;

            }

            path = "E:\\Projects\\VisualStudio\\KinectWpfDemo\\KinectWpfDemo\\Sounds\\";
            string[] sounds = Directory.GetFiles(path, "*.wav");
            triggers = new System.Media.SoundPlayer[sounds.Count()];
            i = 0;
            foreach (var file in sounds)
            {
                triggers[i] = new System.Media.SoundPlayer(file);
                i++;
            }
            boss.Source = images[0];
            boss.Width = images[0].Width;
            boss.Height = images[0].Height;

            projectile_1.Source = images[1];
            projectile_1.Width = images[1].Width;
            projectile_1.Height = images[1].Height;

            projectile_2.Source = images[2];
            projectile_2.Width = images[2].Width;
            projectile_2.Height = images[2].Height;

            projectile_3.Source = images[3];
            projectile_3.Width = images[3].Width;
            projectile_3.Height = images[3].Height;

                   
        }

        public void PlayMusic()
        {
            Uri filePath = new Uri("E:\\Projects\\VisualStudio\\KinectWpfDemo\\KinectWpfDemo\\Music\\distropian_chase.mp3", UriKind.Relative);
            backgroundMusic.Open(filePath);
            backgroundMusic.MediaEnded += new EventHandler(MediaEnded);
            backgroundMusic.Play();

        }
      

        public void StopMusic()
        {
            backgroundMusic.Stop();
        }

        private void MediaEnded(object sender, EventArgs e)
        {
            backgroundMusic.Position = TimeSpan.Zero;
            backgroundMusic.Play();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if(StartStopButton.Content.ToString()=="Start")
            {
                StartStopButton.Content = "Stop";
                if (KinectSensor.KinectSensors.Count > 0)
                {
                    KinectSensor.KinectSensors.StatusChanged += (o, args) =>
                                        {
                                            Status.Content = args.Status.ToString();
                                        };
                    sensor = KinectSensor.KinectSensors[0];
                }
                //score
                label2.Content = "Score: " + score.ToString();
                //soundtrack
                PlayMusic();

                sensor.Start();
                ConnectionId.Content = sensor.DeviceConnectionId;
                //rgb camera detection
                sensor.ColorStream.Enable();             
                sensor.DepthStream.Enable();                
                //skeleton detection
                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;

                sensor.AllFramesReady += sensor_AllFramesReady;

                //MSR
                recognizer = GetRecognizer();
                speechRecognitionEngine = new SpeechRecognitionEngine(recognizer.Id);

                //defining a grammar
                var choices = new Choices();
                choices.Add("level");                               
                var number = new Choices();
                number.Add("one");
                number.Add("two");
                number.Add("three");
        
                //grammar builder;
                var grammarBuilder = new GrammarBuilder { Culture = recognizer.Culture };               
                grammarBuilder.Append(choices);
                grammarBuilder.Append(number);
  
                var grammar = new Grammar(grammarBuilder);
                speechRecognitionEngine.LoadGrammar(grammar);
                speechRecognitionEngine.SpeechRecognized += SpeechRecognitionEngineOnSpeechRecognized;
                //speech thread
                var thread = new Thread(StartAudioStream);
                thread.Start();

            }
            else
            {
                if (sensor!=null && sensor.IsRunning)
                {
                    StopMusic();
                    sensor.Stop();
                    StartStopButton.Content = "Start";
                }
                ImageCanvas.Children.Remove(currentShape);
            }
        }

        private void StartAudioStream()
        {
            speechRecognitionEngine.SetInputToAudioStream(sensor.AudioSource.Start(), new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(EncodingFormat.Pcm,16000,16,1,32000,2,null));
            speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple); 
        }

        private void SpeechRecognitionEngineOnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence < .6)
                return;

            var shapeColor = Colors.Black;
            var shapeKind = Shapes.Square;
            switch (e.Result.Words[0].Text)
            {
                case "level":
                    shapeKind = Shapes.Circle;
                    break;              
              
            }

            switch(e.Result.Words[1].Text)
            {
                case "one":
                    shapeColor = Colors.Green;
                    level = 1;
                    projStatus = false;
                    inLevel = false;
                    levelEnd = true;
                    break;
                case "two":
                    shapeColor = Colors.Blue;
                    level = 2;
                    projStatus = false;
                    inLevel = false;
                    levelEnd = true;
                    break;
                case "three":
                    shapeColor = Colors.White;
                    level = 3;
                    projStatus = false;
                    inLevel = false;
                    levelEnd = true;
                    break;
            }           
            if (currentShape != null)
            {
                ImageCanvas.Children.Remove(currentShape);
                isDrawing = false;
                isSet = false;              
            }
            currentShape = MakeRectangle(shapeColor,shapeKind);
            isDrawing = false;
            isSet = false;
            ImageCanvas.Children.Add(currentShape);
                 
        }

        internal enum Shapes
        {
            Square,
            Rectangle,
            Circle
        }

        private RecognizerInfo GetRecognizer()
        {
            foreach(var recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if("True".Equals(value,StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name,StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }
            return null;
        }

        //main game loop and logic; 30fps instead of time ticker
        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            depthImagePixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength]; 
                   
            //on MotionCanvas paints the input from the regular rgb camera
            using (var frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                {
                    return;
                }
                var bitmap = CreateBitmap(frame);
                MotionCanvas.Background = new ImageBrush(bitmap);
            }

            //on ImageCanvas changes the layer color / everything else remains black
            using (var frame = e.OpenDepthImageFrame())
                  {
                      depthImagePixels = new DepthImagePixel[sensor.DepthStream.FramePixelDataLength];
                      try
                      { 
                           frame.CopyDepthImagePixelDataTo(depthImagePixels);
                      }
                      catch(Exception ex)
                      {
                          Console.WriteLine(ex);
                      }
                      var colorPixels = new byte[4 * sensor.DepthStream.FramePixelDataLength];// 4byte/pixel
           
                      for(int i=0;i<colorPixels.Length;i+=4)
                      {
                          if(depthImagePixels[i/4].PlayerIndex!=0)
                          {
                               if(r>10 || g>10 || b>10)//slider for custom rgb color, by default green
                               {
                                 colorPixels[i + 2] = (byte) r; 
                                 colorPixels[i + 1] = (byte) g;
                                 colorPixels[i + 0] = (byte) b;
                               }
                               else
                              colorPixels[i + spectre] = 255;
                            
                          }
                      }
                      ImageCanvas.Background = new ImageBrush(colorPixels.ToBitmapSource(640, 480));

                //on MotionCanvas draws the polylines to leave color traces
                if (isDrawing == true)
                {
                    trail1.Points.Add(new Point { X = leftHandPoint.X-70, Y = leftHandPoint.Y-20 });
                    trail2.Points.Add(new Point { X = rightHandPoint.X-150, Y = rightHandPoint.Y-20 });
                    trail3.Points.Add(new Point { X = leftFootPoint.X-70, Y = leftFootPoint.Y-20 });
                    trail4.Points.Add(new Point { X = rightFootPoint.X-150, Y = rightFootPoint.Y-20 });
                }
                else if(isSet==false && isDrawing==false)
                {
                    trail1 = new Polyline();
                    trail1.Stroke = System.Windows.Media.Brushes.Red;
                    trail1.StrokeThickness = 5; 
                    trail2 = new Polyline();
                    trail2.Stroke = System.Windows.Media.Brushes.Blue;
                    trail2.StrokeThickness = 5;
                    trail3 = new Polyline();
                    trail3.Stroke = System.Windows.Media.Brushes.Green;
                    trail3.StrokeThickness = 5;
                    trail4 = new Polyline();
                    trail4.Stroke = System.Windows.Media.Brushes.HotPink;
                    trail4.StrokeThickness = 5;
                    isSet = true;
                    MotionCanvas.Children.Clear();
                    MotionCanvas.Children.Add(trail1);
                    MotionCanvas.Children.Add(trail2);
                    MotionCanvas.Children.Add(trail3);
                    MotionCanvas.Children.Add(trail4);
                }               
            }

            //skeleton tracking
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                {
                    return;
                }
                var skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);
                var skeleton = skeletons.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
                if(skeleton== null)
                {
                    return;
                }

                var rightHand = skeleton.Joints[JointType.HandRight].Position;
                var leftHand = skeleton.Joints[JointType.HandLeft].Position;
                var rightLeg= skeleton.Joints[JointType.FootRight].Position;
                var leftLeg = skeleton.Joints[JointType.FootLeft].Position;               

                var mapper = new CoordinateMapper(sensor);
                var colorPoint=mapper.MapSkeletonPointToColorPoint(rightHand, ColorImageFormat.RawBayerResolution640x480Fps30);
                var colorPoint2 = mapper.MapSkeletonPointToColorPoint(rightLeg, ColorImageFormat.RawBayerResolution640x480Fps30);
                var colorPoint3 = mapper.MapSkeletonPointToColorPoint(leftLeg, ColorImageFormat.RawBayerResolution640x480Fps30);
                var colorPoint4 = mapper.MapSkeletonPointToColorPoint(leftHand, ColorImageFormat.RawBayerResolution640x480Fps30);

                //hand/feet positions
                leftHandPoint.X = colorPoint4.X;
                leftHandPoint.Y = colorPoint4.Y;

                rightHandPoint.X = colorPoint.X;
                rightHandPoint.Y = colorPoint.Y;

                rightFootPoint.X = colorPoint2.X;
                rightFootPoint.Y = colorPoint2.Y;

                leftFootPoint.X = colorPoint3.X;
                leftFootPoint.Y = colorPoint3.Y;

                var pointList = new List<ColorImagePoint>();
                pointList.Add(colorPoint);
                pointList.Add(colorPoint2);
                pointList.Add(colorPoint3);
                pointList.Add(colorPoint4);

                BulletBounce(pointList);

                var circle = CreateCircle(colorPoint);               
                LevelStart(colorPoint, circle);

                ProjectileMove();                
            }
        }
        //projectile move
        private void ProjectileMove()
        {
            relativeProjX -= speed * direction;
            isDrawing = true;
            isSet = true;
            try
            {
                foreach (var proj in projectiles)
                {//bullet to canvas margin detection
                    Canvas.SetLeft(proj, relativeProjX);
                    if (relativeProjY < 0 || relativeProjY > ImageCanvas.Height || relativeProjX < 0 || relativeProjX > ImageCanvas.Width)
                    {                        
                        relativeProjY = projectileY;                       
                        relativeProjX = projectileX;                        
                        projStatus = false;
                        Canvas.SetLeft(proj, relativeProjX);
                        Canvas.SetTop(proj, relativeProjY);
                    }
                    //bullet to boss detection
                    if (Canvas.GetLeft(proj) >= Canvas.GetLeft(boss) &&
                                     
                                      Canvas.GetLeft(proj) < (Canvas.GetLeft(boss) + boss.Width)                                     
                                     )
                    {
                        isDrawing = false;
                        isSet = false;
                        score += 1;
                        label2.Content = "Score: " + score.ToString();
                        projStatus = false;
                        direction = 1;
                        triggers[2].Play();
                        Random r = new Random();
                        projectileY = r.Next((int)(bossY), (int)(ImageCanvas.Height - bossY));
                    }
                    if (relativeProjX>ImageCanvas.Width)
                    {
                        direction = 2;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        //level logic
        private void LevelPlay()
        {
            if(inLevel==false && levelEnd==true)
            {
                direction = 1;
                levelEnd = false;
                Canvas.SetLeft(boss, 450);
                Canvas.SetTop(boss, 140);
                ImageCanvas.Children.Add(boss);
                inLevel = true;
                projStatus = false;
                if(level==1)
                {
                    try
                    { 
                        foreach (var x in projectiles)
                        {
                            projectiles.Remove(x);
                        }
                    }catch(Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    projectiles.Add(projectile_1);                    
                    triggers[3].Play();
                    speed = 5;
                    
                    Canvas.SetLeft(projectile_1, projectileX=450);
                    Canvas.SetTop(projectile_1, projectileY=140);

                    ImageCanvas.Children.Add(projectile_1);
                }

                if (level == 2)
                {
                    try
                    {
                        foreach (var x in projectiles)
                        {
                            projectiles.Remove(x);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    projectiles.Add(projectile_2);
                   
                    triggers[4].Play();
                    speed = 10;
                   
                    Canvas.SetLeft(projectile_2, projectileX=450);
                    Canvas.SetTop(projectile_2, projectileY=140);

                    ImageCanvas.Children.Add(projectile_2);
                }

                if (level == 3)
                {
                    try
                    {
                        foreach (var x in projectiles)
                        {
                            projectiles.Remove(x);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    projectiles.Add(projectile_3);
                    triggers[5].Play();
                    speed = 15;                   
                    Canvas.SetLeft(projectile_3, projectileX=450);
                    Canvas.SetTop(projectile_3, projectileY=140);

                    ImageCanvas.Children.Add(projectile_3);
                }
            }
                     
            if (direction>0)
            {
                projStatus = false;
            }
            else
            {
                projStatus = true;
            }                  
        }

        //player to bullet collision method
        private void BulletBounce(List<ColorImagePoint> points)
        {
            foreach(var point in points)
            {
                foreach(var proj in projectiles)
                {
                    if(point.X > Canvas.GetLeft(proj)&&
                       point.X < (Canvas.GetLeft(proj) + proj.Width)&&
                       point.Y > Canvas.GetTop(proj)&&
                       point.Y <(Canvas.GetTop(proj)+proj.Height))
                    {
                        direction =-2;
                        projStatus = true;
                        triggers[5].Play();
                    }
                }
            }
        }


        //start level method; after you select the level this enables you to start
        private void LevelStart(ColorImagePoint colorPoint, Shape circle)
        {
            if (currentShape == null)
                return;

            if(lastMarker!= null)
            {
                ImageCanvas.Children.Remove(lastMarker);
            }

            if(recognizeCount == 0 && 
                colorPoint.X>Canvas.GetLeft(currentShape) && 
                colorPoint.X< (Canvas.GetLeft(currentShape)+currentShape.Width) &&
                colorPoint.Y>Canvas.GetTop(currentShape))
            {
                lastPoint = colorPoint;
                recognizeCount = 1;
                lastMarker = circle;
                ImageCanvas.Children.Add(circle);
                return;
            }

            if(recognizeCount>0 && colorPoint.X > Canvas.GetLeft(currentShape) &&
                colorPoint.X < (Canvas.GetLeft(currentShape) + currentShape.Width) &&
                colorPoint.Y > lastPoint.Y)
            {
                recognizeCount++;
                ImageCanvas.Children.Add(circle);
                lastMarker = circle;
            }
            else
            {
                recognizeCount = 0;
            }
            if(recognizeCount>6)
            {
                ImageCanvas.Children.Remove(currentShape);
                isDrawing = true;
                isSet = true;
                currentShape =null;               
                recognizeCount = 0;
                LevelPlay();
            }

        }

        //level started marker
        private Shape CreateCircle(ColorImagePoint colorPoint)
        {
            var circle = new Ellipse();
            circle.Fill = Brushes.Red;
            circle.Height = 20;
            circle.Width = 20;
            circle.Stroke = Brushes.Red;
            circle.StrokeThickness = 2;

            Canvas.SetLeft(circle, colorPoint.X);
            Canvas.SetTop(circle, colorPoint.Y);

            return circle;
        }

        //shape generator
        private Shape MakeRectangle(Color shapeColor, Shapes shapeKind)
        {
            Shape shape = new Rectangle();
            if(shapeKind==Shapes.Circle)
            {
                shape = new Ellipse();
            }
            
            shape.Stroke = new SolidColorBrush(shapeColor);
            shape.Width =shapeKind==Shapes.Rectangle? 120 : 100;
            shape.Height = 100;
            shape.StrokeThickness = 2;
            shape.Fill = new SolidColorBrush(shapeColor);

            var random = new Random();

            Canvas.SetLeft(shape, 270);
            Canvas.SetTop(shape, 140);
            try
            {
                ImageCanvas.Children.Remove(boss);
                foreach(var proj in projectiles)
                {
                    ImageCanvas.Children.Remove(proj);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return shape;
        }


        private BitmapSource CreateBitmap(ColorImageFrame frame)
        {
            var pixelData = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(pixelData);

            var stride = frame.Width * frame.BytesPerPixel;
            var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null, pixelData, stride);
            return bitmap;
        }
       
  
        //player color choosers
        private void color1_Click(object sender, RoutedEventArgs e)
        {
            spectre = 2;
            redSlider.Value = 0;
            greenSlider.Value = 0;
            blueSlider.Value = 0;
        }

        private void color3_Click(object sender, RoutedEventArgs e)
        {
            spectre = 1;
            redSlider.Value = 0;
            greenSlider.Value = 0;
            blueSlider.Value = 0;
        }
       
        private void color2_Click(object sender, RoutedEventArgs e)
        {
            spectre = 0;
            redSlider.Value = 0;
            greenSlider.Value = 0;
            blueSlider.Value = 0;
        }

        private void redSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            r = (int)redSlider.Value;
        }

        private void greenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            g = (int)greenSlider.Value;
        }

        private void blueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            b = (int)blueSlider.Value;
        }
        //mute background music / unmute background music
        private void MuteUnmuteButton_Click(object sender, RoutedEventArgs e)
        {
            if(MuteUnmuteButton.Content.ToString()=="Mute")
            {
                backgroundMusic.IsMuted = true;
                MuteUnmuteButton.Content = "Unmute";
            }
           else
            {
                backgroundMusic.IsMuted = false;
                MuteUnmuteButton.Content = "Mute";
            }
        }

    }
}
