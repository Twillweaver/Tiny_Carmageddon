using UnityEngine;
using System.IO.Ports;

public class SerialSteering : MonoBehaviour
{
    public string portName = "COM3";
    public int baudRate = 115200;

    SerialPort port;
    public static float steeringValue = 0f;   // -1 to 1

    void Start()
    {
        port = new SerialPort(portName, baudRate);
        port.ReadTimeout = 50;

        try
        {
            port.Open();
        }
        catch
        {
            Debug.LogError("Could not open port " + portName);
        }
    }

    void Update()
    {
        if (port == null || !port.IsOpen) return;

        try
        {
            string line = port.ReadLine().Trim();
            int raw = int.Parse(line);   // 0–1023

            float normalized = raw / 1023f;     // 0 → 1
            steeringValue = Mathf.Lerp(-1f, 1f, normalized);
        }
        catch { }
    }

    void OnApplicationQuit()
    {
        if (port != null && port.IsOpen)
            port.Close();
    }
}
