# ConsoleHangman
A hangman game for the Windows CMD console with several modular elements

--------------

## About this project
This is one of my experimental projects in C#. Experimental, as in what comes out while I'm trying to learn a new language. Most of my experiments have gone down the wrong side of the hill. However, this experiment has gotten to the point where I actually feel like I shouldn't kill it with fire. Yes, there needs to be some effeciency improvements and comments entered in here and there, but this project still claims the best spot in my portfolio (in C#, that is).

## What does it do?
This program is exactly what it sounds like: a Hangman game. You run it in a console and enjoy yourself. However, there is a little more to this program that meets the eye.

### Modular Hangman "Images"
One of the features that really sets this program apart is that the text "Images" that you see while you play are actually loaded from a file that can be changed. This means you can design your own image and what is displayed at every error and whenever you win the game. This adds an interesting modular element to the application where you can add your own personal touch.

### Modular Dictionary File
This is less exciting than the modular images, but you can also select the file that contains the words to be used. This allows you to choose a custom subset of words or terms, which may help you learn those words (maybe).

----------------

## Usage:
    C:\> Hangman.exe ["phrase to guess"] [-i imagefile.hif] [-d dictionaryfile.txt] [-w ["phrase to guess"]]
    C:\> Hangman.exe -p

### `"phrase to guess"`:
Specify a word or phrase to guess. If using spaces, must use quotes. Non-Alphabetical characters appear revealed in puzzle. Overrides `-d`.

### `-i imagefile.hif`:
Specifies a Hangman Image File (`.hif`) to display during the game. The hangman image file defines the amount of incorrect answers before a game is lost. Default is `HangmanImages.hif`. If file cannot be loaded, the game will load without images and will allow the player a hard-coded amount of errors.

### `-d dictionaryfile.txt`:
Specifies a dictionary file (`.txt`) to randomly choose a word from. Default is `words.txt`. Overridden by `-w`.

### `-w ["phrase to guess"]`:
Allows user to specify a word to guess instead of choosing from the dictionary file. If a word or phrase is not specified after, the user must interactively provide a word. Interactive word input hides the text from the screen, which is useful if creating a custom hangman game for someone who may be looking at your screen. Overrides `-d` and a word or phrase typed without `-w`.

### `-p`:
"Prom Mode." Asks the player out on a date with whatever socially awkward hacker dude who runs this cheeky option. Loads whatever's in `debuginfo.hif` as the image file and uses the hard-coded phrase `Will you "hang" with me`. The default `debuginfo.hif` image file makes the game impossible to lose (for the hard-coded phrase) and sets the victory image to an ASCII art of the words `AT PROM?`. Change the victory image to an ASCII art of your date location, such as `"AT FORMAL"` or `"AT THE PARK"`. Overrides all other options. Works 100% of the time (so far, which is one time). This option will be hidden from help menus.
#### NOTE: I DO NOT GUARANTEE DESIRED PERSONAL RELATIONSHIP ADVANCEMENT RESULTS THROUGH USE OF THIS COMMAND OPTION. USE AT YOUR OWN RISK.

----------------

## Contributing
Feel free to contribute whatever you like to this project. But before you do, please read the CONTRIBUTING.MD for important instructions.
