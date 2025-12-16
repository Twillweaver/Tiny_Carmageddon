using UnityEngine;
using System.IO.Ports;

public class SerialSteering : MonoBehaviour
{
    // --- Configurable serial port parameters ---
    public string portName = "COM3";
    public int baudRate = 115200;

    SerialPort port;

    // Public static variable for steering input; ranges from -1 (full left) to 1 (full right)
    public static float steeringValue = 0f;

    void Start()
    {
        // Initialize the SerialPort object with the chosen port and baud rate
        port = new SerialPort(portName, baudRate);

        // Prevents blocking if no data is available
        port.ReadTimeout = 50;

        try
        {
            // Try to open the serial connection
            port.Open();
        }
        catch
        {
            // Warn if connection fails
            Debug.LogError("Could not open port " + portName);
        }
    }

    void Update()
    {
        // Exit early if the port is not open
        if (port == null || !port.IsOpen) return;

        try
        {
            // Read a line of text from the serial port and remove whitespace
            string line = port.ReadLine().Trim();

            // Parse the raw integer from Arduino (assumes 0–1023)
            int raw = int.Parse(line);

            // Normalize to 0–1 range
            float normalized = raw / 1023f;

            // Convert to -1 to 1 range for steering input
            steeringValue = Mathf.Lerp(-1f, 1f, normalized);
        }

        // Ignore read/parse errors (happens if Arduino hasn't sent a full line yet)
        catch { }
    }

    void OnApplicationQuit()
    {
        // Close the serial port safely when the application quits
        if (port != null && port.IsOpen)
            port.Close();
    }
}
