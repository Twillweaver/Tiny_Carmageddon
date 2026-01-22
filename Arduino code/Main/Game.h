#pragma once
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <TM1638plus.h>
#include <Arduino.h>
// Wire.h: I2C for OLED display
// Adafruit_GFX.h + Adafruit_SSD1306.h: For the OLED screen
// TM1638plus.h: For the TM1638 LED+Button module
//Arduino.h: Basic Arduino functions like millis(), pinMode(), digitalRead()


// HARDWARE CONFIGURATION
//
#define OLED_RESET -1
#define STB_PIN D5
#define CLK_PIN D6
#define DIO_PIN D7
#define POT_PIN A0
// Defines pins for hardware:
// OLED_RESET = -1: No reset pin used.
// STB, CLK, DIO: Pins for TM1638 module.
// POT_PIN = A0: Analog input for potentiometer.


// VISUAL & LAYOUT CONSTANTS
//
constexpr int SCREEN_WIDTH = 128;
constexpr int SCREEN_HEIGHT = 64;
constexpr uint8_t OLED_ADDR = 0x3C;
constexpr unsigned long UPDATE_INTERVAL_MS = 50;
constexpr unsigned long SERIAL_INTERVAL_MS = 50;
constexpr unsigned long TM_LED_UPDATE_MS = 1000;
// OLED screen dimensions: 128×64 pixels
// I2C address: 0x3C
// Timing constants:
// UPDATE_INTERVAL_MS = how often the display updates
// SERIAL_INTERVAL_MS = how often Arduino sends data over Serial
// TM_LED_UPDATE_MS = how often TM1638 LEDs update

constexpr uint8_t FONT_WIDTH = 6;
constexpr uint8_t TITLE_Y1 = 18;
constexpr uint8_t TITLE_Y2 = 30;
constexpr uint8_t SPEED_Y = SCREEN_HEIGHT - 12;
constexpr uint8_t COLLECTIBLES_Y = 2;
constexpr int HEART_W = 16;
constexpr int HEART_H = 12;
// Positions and sizes for drawing text and heart sprites on OLED
// Font width for centering text calculations.


// SPRITE HELPER
//
class Sprite {
public:
    static void draw(Adafruit_SSD1306* disp, int x, int y, const uint8_t* bmp, uint8_t w, uint8_t h);
};
// Sprite is a helper for drawing bitmaps on the OLED
// draw() is static because it doesn’t depend on a specific Sprite object


// CAR CLASS
//
class Car {
// Manages the game state for the player’s Car object
public:
    Car();
    void setSpeed(float s);
    void setCollectibles(int c);
    void addCollectible();
    float getSpeed() const;
    int getCollectibles() const;
    void draw(Adafruit_SSD1306* disp);

private:
    float speed;
    int collectibles;
    float lastSpeed;
    int lastCollectibles;
// speed = current speed of Car
// collectibles = number of pick-up items collected
// lastSpeed, lastCollectibles = used to check if the display needs
// to be updated to avoid unnecessary redraws

    int centerX(const char* s, uint8_t fontWidth = FONT_WIDTH) const;
    void drawTitle(Adafruit_SSD1306* disp);
    void drawSpeed(Adafruit_SSD1306* disp);
    void drawCollectibles(Adafruit_SSD1306* disp);
    void drawHearts(Adafruit_SSD1306* disp);
// Helper methods to draw different parts of the UI.
// centerX() calculates horizontal centering for text.

    static constexpr uint8_t heartBmp[24] = {
        0b00011110, 0b01111000,
        0b00111111, 0b11111100,
        0b01111111, 0b11111110,
        0b11111111, 0b11111111,
        0b11111111, 0b11111111,
        0b01111111, 0b11111110,
        0b00111111, 0b11111100,
        0b00011111, 0b11111000,
        0b00001111, 0b11110000,
        0b00000111, 0b11100000,
        0b00000011, 0b11000000,
        0b00000001, 0b10000000
    };
// Hard-coded bitmap for a heart sprite (used in the UI)
};


// GAME CLASS
//
class Game {
// Represents the entire game system: not the most elegant solution,
// but it's servicible
public:
    explicit Game(Adafruit_SSD1306* d, TM1638plus* t, int pot);
// constructor for the Game Class holds references to hardware objects and a Car instance
// explicit prevents the constructor from being used for implicit type conversions
    void begin();
// initializes hardware
    void update(unsigned long nowMs);
// update() = main game loop logic
// unsigned long avoids negative time values
// nowMS = current moment, measured in milliseconds

private:
    Adafruit_SSD1306* display;
    TM1638plus* tm;
    int potPin;
    Car car;
    uint8_t lastButtons;
    unsigned long lastUpdateMs;
    unsigned long lastSerialMs;
    int lastPotValue;
    uint8_t lastButtonState;
    unsigned long lastTMLEDUpdate;
// Keeps track of:
// - Last button states
// - Timing for updates (display, serial, LEDs)
// - Last potentiometer value

    struct UnityMessage { float speed; int collectibles; };
    bool readUnityData(UnityMessage& msg);
    void updateTMDisplay();
    void writeTMText(const char* s);
    void updateTMLEDs(int count);
    void setLEDMask(uint8_t mask);
    void sendToUnity(int potValue, uint8_t buttons);
// Methods for:
// readUnityData = Reading data from Unity via Serial
// updateTMDisplay, updateTMLEDs = Updating TM1638 display and LEDs
// sendToUnity = Sending user inputs back to Unity
};


// FACTORY FUNCTION
//
Game& getGameInstance();
// Factory function returning a singleton Game object
// ensuring only one instance exists
