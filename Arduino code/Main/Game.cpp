#include "Game.h"
#include <math.h>


// SPRITE IMPLEMENTATION
//
void Sprite::draw(Adafruit_SSD1306* disp, int x, int y, const uint8_t* bmp, uint8_t w, uint8_t h) {
    if (disp) disp->drawBitmap(x, y, bmp, w, h, SSD1306_WHITE);
// Draws a bitmap on the OLED at (x, y) if the display is valid
}


// CAR IMPLEMENTATION
//
Car::Car() : speed(0.0f), collectibles(0), lastSpeed(-1.0f), lastCollectibles(-1) {}
// Constructor initialises values and sets “last values” to impossible defaults to force initial redraw
void Car::setSpeed(float s) { speed = constrain(s, 0.0f, 999.0f); }
// Limits speed to 0–999 to avoid overflow
// constrain() function is built into Arduino (value, min, max)
void Car::setCollectibles(int c) { collectibles = max(0, min(c, 255)); }
// Limits collectibles to 0–255, which fits in one byte for TM1638 LEDs
void Car::addCollectible() { if (collectibles < 255) collectibles++; }
// Increments collectibles safely and guards against overflow
float Car::getSpeed() const { return speed; }
int Car::getCollectibles() const { return collectibles; }
// Getters for encapsulation of values
// const keeps them from modifying the Car

int Car::centerX(const char* s, uint8_t fontWidth) const { return (SCREEN_WIDTH - strlen(s) * fontWidth) / 2; }
// Calculates horizontal position to center text

void Car::draw(Adafruit_SSD1306* disp) {
// Draws the entire UI on the OLED: title, speed, collectibles, heart bitmaps
    if (!disp) return;
// If the display is missing or failed = do nothing
    bool changed = fabsf(speed - lastSpeed) > 0.05f || collectibles != lastCollectibles;
// This is the line that uses math.h
// It returns the absolute value of a float
// If speed changed by more than 0.05 units, the screen is redrawn
    if (!changed) return;
// Optimisation: If the display hasn't changed = do not redraw


// Prepare the display
    disp->clearDisplay();
    disp->setTextSize(1);
    disp->setTextColor(SSD1306_WHITE);
// Wipes the entire screen buffer
// 1 = smallest legible size for this font
// Text will be drawn in white pixels


// Draw four UI components
    drawTitle(disp);
    drawSpeed(disp);
    drawCollectibles(disp);
    drawHearts(disp);
// Each helper function adds its portion of the UI

    disp->display();
// Until this moment the drawing only exists in an internal RAM buffer
// display() pushes it to the OLED

// Remember the current values
    lastSpeed = speed;
    lastCollectibles = collectibles;
// Next time draw() is called, the code compares new values to these
// If they haven't changed = skip redraw
}

// Helper functions
void Car::drawTitle(Adafruit_SSD1306* disp) {
    disp->setCursor(centerX("TOY"), TITLE_Y1);
    disp->print("TOY");
    disp->setCursor(centerX("CARMAGEDDON!"), TITLE_Y2);
    disp->print("CARMAGEDDON!");
// centerX() figures out the x-position needed to center text
}

void Car::drawSpeed(Adafruit_SSD1306* disp) {
    char buf[8];
    snprintf(buf, sizeof(buf), "%.1f", speed);
    disp->setCursor(centerX(buf), SPEED_Y);
    disp->print(buf);
// Formats speed into "12.3" (1 decimal place)
// Then prints it centered using centerX(buf)
}

void Car::drawCollectibles(Adafruit_SSD1306* disp) {
    char buf[8];
    snprintf(buf, sizeof(buf), "x%d", collectibles);
    disp->setCursor(centerX(buf), COLLECTIBLES_Y);
    disp->print(buf);
// Formats output like this: "x3"
// Then prints it centered using centerX(buf)
}

void Car::drawHearts(Adafruit_SSD1306* disp) {
    Sprite::draw(disp, 0, 0, heartBmp, HEART_W, HEART_H);
    Sprite::draw(disp, SCREEN_WIDTH - HEART_W, 0, heartBmp, HEART_W, HEART_H);
    Sprite::draw(disp, 0, SCREEN_HEIGHT - HEART_H, heartBmp, HEART_W, HEART_H);
    Sprite::draw(disp, SCREEN_WIDTH - HEART_W, SCREEN_HEIGHT - HEART_H, heartBmp, HEART_W, HEART_H);
// Draws a heart bitmap from a hardcoded array
// Called four times to place hearts in all corners
}


// GAME IMPLEMENTATION
//
Game::Game(Adafruit_SSD1306* d, TM1638plus* t, int pot)
    : display(d), tm(t), potPin(pot),
      lastButtons(0), lastUpdateMs(0), lastSerialMs(0),
      lastPotValue(-1), lastButtonState(0), lastTMLEDUpdate(0) {}
// Constructor initializes hardware references and timing variables
// This sets the initial values for member variables before the constructor body runs
// It's more efficient than assigning inside the constructor
// By starting potentiometer with -1, the first read is guaranteed to be “different”,
// so the program sends the initial value immediately


// Starts Serial communication with Unity:
void Game::begin() {
    Serial.begin(115200);
    Wire.begin(D2, D1);
    delay(50);

//  Initialises OLED module
    if (display && display->begin(SSD1306_SWITCHCAPVCC, OLED_ADDR)) {
// Checks if the OLED pointer exists and tries to initialize it
// SSD1306_SWITCHCAPVCC tells the library to use internal voltage switching
        display->setRotation(2);
// Flips the display upside down
        display->clearDisplay();
// Clears the internal buffer
        display->display();
// Pushes the buffer to the OLED
    } else {
        Serial.println("ERR: OLED init failed");
        if (tm) writeTMText("OLEDFAIL");
// OLED errors are pushed to TM1638 display
    }

// Initialises TM1638 module
    if (tm) {
        tm->displayBegin();
// Pointer check = only inititialise if TM is non-null
        delay(50);
        tm->brightness(7);
        writeTMText("---");
// Flashes “---” on TM1638 if hardware is present
        updateTMLEDs(0);
// Initial state = turns all LEDs off
    }
}


// Main loop:
void Game::update(unsigned long nowMs) {
    if (nowMs - lastUpdateMs < UPDATE_INTERVAL_MS) return;
    lastUpdateMs = nowMs;
// Throttle update frequency
// unsigned long prevents unexpected negative values

    int potValue = analogRead(potPin);
    uint8_t buttons = tm ? tm->readButtons() & 0xFF : 0;
// analogRead = Read potentiometer
// readButtons = Read TM1638 buttons
// ? = conditional operator, modern compact way to write if-else statement:
// if TM is not null, call readButtons; else set it to 0

    UnityMessage msg;
    if (readUnityData(msg)) {
        car.setSpeed(msg.speed);
        car.setCollectibles(msg.collectibles);
// Reads incoming messages from Unity over Serial
    }


    car.draw(display);
// This redraws the OLED screen only if something has changed
    updateTMDisplay();
// Updates the 7-segment display on the TM1638
    updateTMLEDs(car.getCollectibles());
// Updates the 8 LEDs on the TM1638 to reflect how many collectibles the Player has

// Sends Arduino inputs to Unity
// Two conditions must be true before sending:
// - Enough time has passed
// - At least one value has changed
    if (nowMs - lastSerialMs >= SERIAL_INTERVAL_MS &&
        (potValue != lastPotValue || buttons != lastButtonState)) {
        sendToUnity(potValue, buttons);
        lastSerialMs = nowMs;
        lastPotValue = potValue;
        lastButtonState = buttons;
// Updates memory of last-sent values
    }

    lastButtons = buttons;
}


bool Game::readUnityData(UnityMessage& msg) {
    if (Serial.available() <= 0) return false;
// If no data is available = stop
    char buf[48];
    int len = Serial.readBytesUntil('\n', buf, sizeof(buf) - 1);
    if (len <= 0) return false;
    buf[len] = '\0';
// Reads a line from Serial (e.g. "14.5,8\n")

    char* comma = strchr(buf, ',');
    if (!comma || comma == buf) return false;
// Finds a comma
// No comma = error

    *comma = '\0';
    msg.speed = atof(buf);
    msg.collectibles = atoi(comma + 1);
    return !isnan(msg.speed) && msg.collectibles >= 0;
// Splits the incoming value at the comma
// Converts the speed to float & collectibles to int
// Validates and returns success
}


// Updates TM1638:
void Game::updateTMDisplay() {
// updateTMDisplay() = display car speed as a floating number
    if (!tm) return;
// If TMM1638 is missing = exit
    char buf[9];
// buf = buffer
// Since microcontrollers can't use fancy dynamic string classes,
// we can store numbers in an array
// In this case: 8 characters and the null terminator 
    snprintf(buf, sizeof(buf), "%6.1f", car.getSpeed());
// Formats the speed as a string
    writeTMText(buf);
}

// Writing to the 7-segment display
void Game::writeTMText(const char* s) {
    if (!tm) return;
    for (uint8_t i = 0; i < 8; i++) {
// Loops over all 8 positions of the 7-segment display
        char c = s[i];
        if (c == '\0') c = ' ';
// If the string is shorter, the rest are blank
        tm->displayASCII(i, (uint8_t)c, (TM1638plus::DecimalPoint_e)0);
// Library automatically converts ASCII to segments
    }
}

// Displays collectibles as binary via TM1638 LEDs
void Game::updateTMLEDs(int count) {
    if (!tm) return;
    unsigned long now = millis();
    if (now - lastTMLEDUpdate < TM_LED_UPDATE_MS) return;
// Throttling: reduces flicker and CPU load
    lastTMLEDUpdate = now;

// Converts collectibles to LED bits
    uint8_t mask = static_cast<uint8_t>(count & 0xFF);
    setLEDMask(mask);
// Sends mask to TM1638
}

// Takes an 8-bit mask and updates the 8 LEDs on the TM1638 module
 void Game::setLEDMask(uint8_t mask) {
    for (uint8_t i = 0; i < 8; i++) {
// Loops over all 8 LEDs individually
        tm->setLED(i, (mask >> i) & 0x01);
// Uses the TM1638 library function to turn LED i on or off
// Right-shifts the mask by i bits
// Isolates the least significant bit after the shift
    }
}

void Game::sendToUnity(int potValue, uint8_t buttons) {
    Serial.print(potValue);
    Serial.print(",");
    Serial.println(buttons);
// Sends a line "potValue,buttons\n" back to Unity for input processing
}


// SINGLETON FACTORY FUNCTION
// ---
Game& getGameInstance() {
    static Adafruit_SSD1306 display(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);
    static TM1638plus tm(STB_PIN, CLK_PIN, DIO_PIN, false);
    static Game game(&display, &tm, POT_PIN);
    return game;
// Ensures one global instance of the Game, OLED display and TM1638 module
}
