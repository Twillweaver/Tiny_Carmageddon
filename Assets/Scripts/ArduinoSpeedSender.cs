using UnityEngine;
using System.IO.Ports;

public class ArduinoSpeedSender : MonoBehaviour
{
    public PlayerController_Arduino car; // Assign car object
    public string portName = "COM3";
    public int baudRate = 115200;

    SerialPort port;

    void Start()
    {
        port = new SerialPort(portName, baudRate);
        port.Open();
    }

    void Update()
    {
        if (port != null && port.IsOpen)
        {
            float speed = car.GetComponent<Rigidbody>().linearVelocity.magnitude; // m/s
            port.WriteLine(speed.ToString("F1")); // Send as string with 1 decimal
        }
    }

    void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
