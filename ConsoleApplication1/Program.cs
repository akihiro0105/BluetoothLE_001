using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

using System.Net.Sockets;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {

        private static float pitch, roll,yaw;

        static void Main(string[] args)
        {
            pitch = 0;
            roll = 0;
            yaw = 0;

            Task task = Task.Factory.StartNew(async () => bluetoothle());

            Connect();

            task.Wait();

            while (Console.ReadLine() != "exit") { }
        }

        static public async void bluetoothle()
        {
            //デバイスを検索
            string deveiceSelector = GattDeviceService.GetDeviceSelectorFromUuid(GattServiceUuids.HealthThermometer);//uuidから取得
            DeviceInformationCollection themometerServices = await DeviceInformation.FindAllAsync(deveiceSelector, null);
            Console.WriteLine(themometerServices[0].Name);
            Console.ReadLine();

            //デバイの指定
            if (themometerServices.Count > 0)
            {
                DeviceInformation themometerService = themometerServices.First();
                string ServiceNameText = "Using service: " + themometerService.Name;

                // サービスを作成
                GattDeviceService firstThermometerService = await GattDeviceService.FromIdAsync(themometerService.Id);

                if (firstThermometerService != null)
                {
                    //Gattの選択
                    // キャラクタリスティックを取得
                    GattCharacteristic thermometerCharacteristic = firstThermometerService.GetCharacteristics(GattCharacteristicUuids.TemperatureMeasurement).First();

                    // 通知イベントを登録

                    Console.WriteLine("Connect:"+ ServiceNameText + "\n");

                    await thermometerCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);

                    thermometerCharacteristic.ValueChanged += TemperatureMeasurementChanged;

                }
                else
                {
                    // サービスを見つけられなかった
                    // Capabilityの設定漏れはここへ
                    Console.WriteLine("Notfound:" + ServiceNameText + "\n");
                    return;
                }
            }
            else
            {
                // 発見できなかった
                // BluetoothがOFFの場合はここへ
                Console.WriteLine("Notfound : Bluetooth"  + "\n");
                return;
            }
        }

        //値の取得
        static private void TemperatureMeasurementChanged(GattCharacteristic sender, GattValueChangedEventArgs eventArgs)
        {
            byte[] temperatureData = new byte[eventArgs.CharacteristicValue.Length];
            Windows.Storage.Streams.DataReader.FromBuffer(eventArgs.CharacteristicValue).ReadBytes(temperatureData);
            float calx =  BitConverter.ToSingle(temperatureData, 0);
            float caly = BitConverter.ToSingle(temperatureData, 4);
            float calz =  BitConverter.ToSingle(temperatureData, 8);
            pitch = calx;
            roll = caly;
            yaw = calz;
            Console.WriteLine("count:" + temperatureData.Length.ToString() + " " + calx.ToString("0.00") + " " + caly.ToString("0.00") + " " + calz.ToString("0.00"));
        }


        static void Connect()
        {
            try
            {
                //サーバーを開始 
                Int32 port = 9999;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                TcpListener Server = new TcpListener(localAddr, port);
                Server.Start();

                while (true)
                {
                    //接続待機 
                    Console.WriteLine("接続待機中");
                    TcpClient client = Server.AcceptTcpClient();

                    //接続 
                    Console.WriteLine("接続されました");
                    NetworkStream stream = client.GetStream();

                    Byte[] bytes = new Byte[256];
                    int siz;

                    //メッセージを受信 
                    while ((siz = stream.Read(bytes, 0, bytes.Length)) != 0)
                    //while(true)
                    {
                        byte[] buf1 = new byte[4];
                        buf1 = BitConverter.GetBytes(pitch);
                        byte[] buf2 = new byte[4];
                        buf2 = BitConverter.GetBytes(roll);
                        byte[] buf3 = new byte[4];
                        buf3 = BitConverter.GetBytes(yaw);
                        byte[] msg = new byte[12];
                        msg[0] = buf1[0];
                        msg[1] = buf1[1];
                        msg[2] = buf1[2];
                        msg[3] = buf1[3];
                        msg[4] = buf2[0];
                        msg[5] = buf2[1];
                        msg[6] = buf2[2];
                        msg[7] = buf2[3];
                        msg[8] = buf3[0];
                        msg[9] = buf3[1];
                        msg[10] = buf3[2];
                        msg[11] = buf3[3];

                        // 送信(データサイズは，UTF8のサイズ
                        stream.Write(msg, 0, msg.Length);
                    }

                    stream.Close();
                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
