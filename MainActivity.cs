using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Android.Gms.Vision;
using Android.Gms.Vision.Texts;
using Android.Util;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.App;
using Android;
using Android.Content.PM;
using static Android.Gms.Vision.Detector;
using System.Text;
using System;
using MySqlConnector;

namespace AS
{
    [Activity(Label = "CameraTextRecognizer", MainLauncher = true)]
    public class MainActivity : Activity, ISurfaceHolderCallback, IProcessor
    {
        private SurfaceView cameraView;
        private TextView txtView;
        private TextView txtView2;
        private CameraSource cameraSource;
        private
        const int RequestCameraPermissionID = 1001;
        public string str;
        public int num;
        public bool flag = false;
        public string date;
        public string time;
        public string day;
        public string db_cab;
        public string db_time;
        public string db_date;
        public string db_teacher;
        public string db_class;
        public string db_lesson;
        ImageButton button1;
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            switch (requestCode)
            {
                case RequestCameraPermissionID:
                    {
                        if (grantResults[0] == Permission.Granted)
                        {
                            cameraSource.Start(cameraView.Holder);
                        }
                    }
                    break;
            }
        }
        


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource  
            SetContentView(Resource.Layout.Main);
            button1 = FindViewById<ImageButton>(Resource.Id.btn1);
            button1.Touch += button1_click;
            cameraView = FindViewById<SurfaceView>(Resource.Id.surface_view);
            txtView = FindViewById<TextView>(Resource.Id.txtview); 
            TextRecognizer txtRecognizer = new TextRecognizer.Builder(ApplicationContext).Build();
            if (!txtRecognizer.IsOperational)
            {
                Log.Error("Main Activity", "Detector dependencies are not yet available");
            }
            else
            {
                cameraSource = new CameraSource.Builder(ApplicationContext, txtRecognizer).SetFacing(CameraFacing.Back).SetRequestedPreviewSize(1280, 1024).SetRequestedFps(2.0f).SetAutoFocusEnabled(true).Build();
                cameraView.Holder.AddCallback(this);
                txtRecognizer.SetProcessor(this);
            }
        }

        void button1_click(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                button1.SetImageResource(Resource.Drawable.btn_pressed);
            }
            else if (e.Event.Action == MotionEventActions.Up)
            {
                button1.SetImageResource(Resource.Drawable.btn_default);
            }
            Scan(str);
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height) { }
        public void SurfaceCreated(ISurfaceHolder holder)
        {
            if (ActivityCompat.CheckSelfPermission(ApplicationContext, Manifest.Permission.Camera) != Android.Content.PM.Permission.Granted)
            {
                //Request permission  
                ActivityCompat.RequestPermissions(this, new string[] {
                    Android.Manifest.Permission.Camera
                }, RequestCameraPermissionID);
                return;
            }
            cameraSource.Start(cameraView.Holder);
        }
        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            cameraSource.Stop();
        }
        public void ReceiveDetections(Detections detections)
        {
            SparseArray items = detections.DetectedItems;
            if (items.Size() != 0)
            {
                txtView.Post(() => {
                    StringBuilder strBuilder = new StringBuilder();
                    strBuilder.Append(((TextBlock)items.ValueAt(0)).Value);
                    strBuilder.Append("\n");
                    str = strBuilder.ToString();
                    string str_num = "";
                    for (int i = 0; i < str.Length; i++)
                        if (str[i] >= '0' && str[i] <= '9')
                        {
                            str_num += str[i].ToString();
                        }
                    txtView.Text = str_num;
                    flag = true;
                });
            }
        }
        public void Scan(string s) {
            if (flag)
            {
                string str_num = "";
                for (int i = 0; i < s.Length; i++)
                    if (s[i] >= '0' && s[i] <= '9')
                    {
                        str_num += s[i].ToString();
                    }
                if (str_num.Length == 2)
                {
                    DateTime dt = DateTime.Now;
                    string weekday = dt.ToString("ddd");
                    string hour = dt.Hour.ToString();
                    string minutes = dt.Minute.ToString();
                    int h = Convert.ToInt32(hour);
                    int m = Convert.ToInt32(minutes);
                    try
                    {
                        string connStr = "server=192.162.244.38;user=sql_9_free;database=sql_9_free;password=123456;";
                        // создаём объект для подключения к БД
                        MySqlConnection conn = new MySqlConnection(connStr);
                        // устанавливаем соединение с БД
                        conn.Open();
                        bool peremena = true;
                        // запрос
                        string sql = "SELECT * FROM scan_lab";
                        // объект для выполнения SQL-запроса
                        MySqlCommand command = new MySqlCommand(sql, conn);
                        // объект для чтения ответа сервера
                        MySqlDataReader reader = command.ExecuteReader();
                        //читаем результат
                        int t = h * 60 + m;
                        while (reader.Read())
                        {
                            db_cab = reader[1].ToString();
                            db_time = reader[4].ToString();
                            db_date = reader[5].ToString();
                            string[] timeSplit = db_time.Split('.', '-');
                            int t1 = Convert.ToInt32(timeSplit[0]) * 60 + Convert.ToInt32(timeSplit[1]);
                            int t2 = Convert.ToInt32(timeSplit[2]) * 60 + Convert.ToInt32(timeSplit[3]);
                            //int t1 = Convert.ToInt32(db_time[0].ToString() + db_time[1].ToString())*60 + Convert.ToInt32(db_time[3].ToString() + db_time[4].ToString());
                            //int t2 = Convert.ToInt32(db_time[6].ToString() + db_time[7].ToString())*60 + Convert.ToInt32(db_time[9].ToString() + db_time[10].ToString());
                            if (str_num == db_cab)
                                if (weekday == db_date)
                                    if ((t > t1) && (t < t2))
                                    {
                                        db_teacher = reader[2].ToString();
                                        db_class = reader[3].ToString();
                                        db_lesson = reader[6].ToString();
                                        peremena = false;
                                        break;
                                    }
                        }
                        // закрываем reader
                        reader.Close();
                        // закрываем соединение с БД
                        conn.Close();
                        if (peremena == false)
                        {
                            string msg = "Время:\t"+db_time + "\t" + db_date + "\nУрок:\t" + db_lesson + "\nУчитель:\t" + db_teacher + "\nКабинет:\t" + db_cab + "\nКласс:\t" + db_class;
                            if (db_lesson == "Математика") { 
                            AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                            alertDialog.SetTitle("Что в классе?");
                            alertDialog.SetMessage(msg);
                            alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                            alertDialog.SetIcon(Resource.Drawable.m1);
                            alertDialog.Show();
                            }
                            else if ((db_lesson == "Русский язык") || (db_lesson== "Родной язык"))
                            {
                                AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                                alertDialog.SetTitle("Что в классе?");
                                alertDialog.SetMessage(msg);
                                alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                                alertDialog.SetIcon(Resource.Drawable.r1);
                                alertDialog.Show();
                            }
                            else
                            {
                                AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                                alertDialog.SetTitle("Что в классе?");
                                alertDialog.SetMessage(msg);
                                alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                                alertDialog.SetIcon(Resource.Drawable.Classik);
                                alertDialog.Show();
                            }
                        }
                        else { 
                            string msg = "В данный момент в это кабинете ничего не происходит";
                            AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                            alertDialog.SetTitle("Что в классе?");
                            alertDialog.SetMessage(msg);
                            alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                            alertDialog.SetIcon(Resource.Drawable.nth);
                            alertDialog.Show();
                        }
                    }
                    catch (Exception ex)
                    {
                        string msg = "Не удалось подключиться к базе данных повтори еще разок ;)";
                        AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                        alertDialog.SetTitle("ERROR");
                        alertDialog.SetMessage(msg);
                        alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                        alertDialog.SetIcon(Resource.Drawable.Krestik);
                        alertDialog.Show();
                    }
                }
                else if (str_num.Length == 4)
                {
                    string msg = "Подожди, где ты учишься?\nЯ такой номер от кабинета в жизни не видел...\nМожешь поделиться скрином в нашей группе вк))\n https://vk.com/public202538571";
                    AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                    alertDialog.SetTitle("Easter Egg");
                    alertDialog.SetMessage(msg);
                    alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                    alertDialog.SetIcon(Resource.Drawable.Eggs);
                    alertDialog.Show();
                }
                else
                {
                    string msg = "Ошибка распознования числа(должно быть число, состоящее из 2 цифр)";
                    AlertDialog.Builder alertDialog = new AlertDialog.Builder(this);
                    alertDialog.SetTitle("Help");
                    alertDialog.SetMessage(msg);
                    alertDialog.SetNeutralButton("OK", delegate { alertDialog.Dispose(); });
                    alertDialog.SetIcon(Resource.Drawable.Krestik);
                    alertDialog.Show();
                }
            }
        }
        public void Release() { }
    }
}