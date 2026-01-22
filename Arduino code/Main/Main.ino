#include "Game.h"
// Pulls in definitions for Game, Car, and Sprite classes, along with all constants and hardware configurations

void setup() {
    getGameInstance().begin();
// Calls a singleton factory function
// Initializes Serial communication at 115200 baud for talking to Unity
// Sets up the I2C bus (Wire.begin) for the OLED display
// Initializes the OLED display and clears it
// Initializes the TM1638 module and sets initial brightness
}

void loop() {
    getGameInstance().update(millis());
// Updates the game state every iteration
// Ensures the same singleton game instance is running
// The millis() function provides the current time in milliseconds since Arduino started,
// which is used to manage timing without blocking execution
// update() throttles to avoid running too fast using UPDATE_INTERVAL_MS
// Reads inputs from the potentiometer and TM1638 buttons
// Sends input data to Unity if changed
// Updates the OLED display and TM1638 LEDs for speed and collectibles
// Updates the Car object based on Unity data or button presses
}
