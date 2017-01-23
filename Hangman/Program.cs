using System;
using System.IO;
using System.Threading;	// May be used in final release

namespace Hangman
{
	class MainClass
	{
		// Class variables for Command line stuff
		static string version = "0.7.8";
		static readonly ConsoleColor defaultColor = Console.ForegroundColor;	// color to change back to after color is changed

		// Instance variables to reduce amount of parameters needed to type
		Hangman hangman;
		HangmanImageProcessor hip;
		HangmanDictionaryFileProcessor hdfp;
		bool firstPass;		// used by various methods. Is true when the status code should be set to zero


		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hangman.MainClass"/> class.
		/// </summary>
		/// <param name="hip">Hifp.</param>
		/// <param name="hdfp">Hdfp.</param>
		MainClass(HangmanImageProcessor hip, HangmanDictionaryFileProcessor hdfp)
		{
			this.hip = hip;
			this.hdfp = hdfp;
			hangman = new Hangman(hdfp.getRandomWord());
			firstPass = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hangman.MainClass"/> class.
		/// </summary>
		/// <param name="hangman">Hangman.</param>
		/// <param name="hip">Hip.</param>
		MainClass(Hangman hangman, HangmanImageProcessor hip)
		{
			this.hangman = hangman;
			this.hip = hip;
			hdfp = null;
			firstPass = true;
		}

		public static void Main (string[] args)
		{
			// test client code

			try
			{
				var mc = handleArguments(args);

				mc.runHangman();
			}
			catch (Exception e)
			{
				displayException(e);
			}
		}

		/// <summary>
		/// Handles the arguments.
		/// </summary>
		/// <returns>a MainClass object representing the objects created.</returns>
		/// <param name="args">Arguments the argument array.</param>
		public static MainClass handleArguments(string[] args)
		{
			// Initialize game objects
			Hangman hangman = null;
			HangmanImageProcessor hip = null;
			HangmanDictionaryFileProcessor hdfp = null;

			// Initialize other variables
			bool isCustom = false;
			int currentArg = 0;
			bool exceptionThrown = false;
			bool needToPause = false;

			// Traverse the arguments
			while (currentArg < args.Length)
			{
				// check the argument
				switch (args[currentArg])
				{
					case "-i":
					case "-imageFile":
						{
							currentArg++;

							if (currentArg < args.Length)
							{
								// create image file processor from arguments
								try
								{
									hip = new HangmanImageFileProcessor(args[currentArg]);
								}
								catch (Exception e)
								{
									displayException(e);
									needToPause = true;
									hip = null;
								}
							}
							else
							{

								// -i did not have anything to follow it
								throw new FormatException("Image File Location was not specified.");
							}
							break;
						}

					case "-d":
					case "-DictionaryFile":
						{
							currentArg++;

							try
							{
								if (currentArg < args.Length)
								{
									// create dictionary file processor from arguments
									hdfp = new HangmanDictionaryFileProcessor(args[currentArg]);
								}
								else {
									// -d did not have anything to follow it
									throw new FormatException("Dictionary File location was not specified.");
								}
								break;
							}
							catch (Exception e)
							{
								displayException(e);
								exceptionThrown = true;
								hdfp = null;
								needToPause = true;
							}

							break;
						}

					case "-w":
					case "-Word":
						{
							isCustom = true;
							currentArg++;
							if (currentArg < args.Length &&
								HangmanDictionaryFileProcessor
								.checkStringLettersOnly(args[currentArg]))
							{
								hangman = new Hangman(args[currentArg]);
							}

							break;
						}

					default:
						{
							if (hangman == null && HangmanDictionaryFileProcessor.checkStringLettersOnly(args[currentArg]))
							{
								// create Hangman using argument as word
								hangman = new Hangman(args[currentArg]);
								isCustom = true;
							}
							else if (hangman == null) 
							{
								// Word was not formatted correctly
								throw new FormatException("Default Word argument contained illegal character(s)");
							}

							break;
						}
				}

				currentArg++;
			}

			// Check whether defaults should be used
			if (hip == null)
			{
				Console.WriteLine("Using Default image file . . .");
				try
				{
					hip = new HangmanImageFileProcessor(HangmanImageFileProcessor.DEFAULT_LOCATION);
				}
				catch (Exception e)
				{
					displayException(e);
					exceptionThrown = true;
					hip = null;
					needToPause = true;
				}

				if (exceptionThrown)
				{
					Console.WriteLine("Using Hardcoded Image . . .");
					hip = new DefaultHangmanImageProcessor(7);
				}
			}

			if (!isCustom)
			{
				if (hdfp == null)
				{
					try
					{
						Console.WriteLine("Using default Dictionary file . . .");
						hdfp = new HangmanDictionaryFileProcessor(HangmanDictionaryFileProcessor.DEFAULT_LOCATION);
					}
					catch (Exception e)
					{
						displayException(e);
						needToPause = true;
						hdfp = null;
						throw;
					}
				}

				if (needToPause)
				{
					Console.WriteLine("Press any key to continue. . .");
					Console.ReadKey(true);
				}

				return new MainClass(hip, hdfp);
			}
			else 
			{
				string word = "";
				bool isWord = true;

				if (hangman == null)
				{
					do
					{
						if (!isWord)
						{
							Console.WriteLine("\nThat's not an acceptable word! Try again.");
							word = "";
						}

						Console.Write("Enter word to guess (Don't worry, it's hidden): ");

						ConsoleKeyInfo keyPressed;

						while ((keyPressed = Console.ReadKey(true)).Key != ConsoleKey.Enter)
						{
							word += keyPressed.KeyChar;
						}

					}
					while (!(isWord = HangmanDictionaryFileProcessor.checkStringLettersOnly(word)));

					hangman = new Hangman(word);
				}

				if (needToPause)
				{
					Console.WriteLine("Press any key to continue. . .");
					Console.ReadKey(true);
				}

				return new MainClass(hangman, hip);
			}
		}

		/// <summary>
		/// Runs the hangman game.
		/// </summary>
		public void runHangman()
		{
			int statusCode;
			while (!isGameSuspended((statusCode = findNextStatusCode())))
			{
				updateInterface(statusCode);
			}

			updateInterface(statusCode);

			if (statusCode == -1)
			{
				var input = Console.ReadKey(true);

				if (input.Key == ConsoleKey.Y)
				{
					Console.Clear();
					return;	// Exits the program
				}

				runHangman();   // Runs this method again.
				return;	// Makes sure no further code is executed after recursion is complete
			}

			Console.ReadKey(true);
			Console.Clear();
		}

		/// <summary>
		/// Finds the next status code.
		/// </summary>
		/// <returns>The next status code.</returns>
		public int findNextStatusCode()
		{
			if (firstPass)
			{
				firstPass = false;
				return 0;
			}

			char input = getInput();

			if (Char.IsLetter(input))
			{
				int wrongBefore = hangman.getErrors();

				string lettersGuessed = hangman.getLettersGuessed();

				if (lettersGuessed.Contains("" + Char.ToUpper(input)))
				{
					return 4;
				}

				string state = hangman.makeGuess(input);

				if (wrongBefore == hangman.getErrors())
				{

					if (hangman.getVictory())
					{
						return 8;
					}

					return 1;
				}

				if (hangman.getErrors() == hip.getNumImages() - 2)
				{
					return 9;
				}

				return 2;
			}

			if (input == '*')	// User wants to exit
			{
				firstPass = true;
				return -1;
			}

			return 3;	// Invalid input
		}

		/// <summary>
		/// Gets the input from the command line.
		/// </summary>
		/// <returns>A character representing what was pressed</returns>
		public static char getInput()
		{
			var keyPressed = Console.ReadKey(true);

			if (isKeyALetter(keyPressed.Key))
			{
				return keyPressed.KeyChar;
			}
			if (keyPressed.Key == ConsoleKey.Escape)
			{
				return '*';	// an Asterisk will mean the user wants to exit. Prompt using updateInterface(-1);
			}

			return '~'; // a Tilde will mean the user entered an incorrect key. Prompt using updateInterface(3);
		}

		/// <summary>
		/// Determines if the key is a letter.
		/// </summary>
		/// <returns><c>true</c>, if key is a letter, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public static bool isKeyALetter(ConsoleKey key)
		{
			return key >= ConsoleKey.A && key <= ConsoleKey.Z;
		}

		/// <summary>
		/// Updates the user interface.
		/// </summary>
		/// <param name="statusCode">Status code of the Status message.</param>
		public void updateInterface(int statusCode)
		{
			// Clear Console and set Cursor to top left
			Console.Clear();
			Console.SetCursorPosition(0, 0);

			// Print Version Line
			Console.WriteLine("Console Hangman v{0} ([ESC] to exit)", version);
			Console.WriteLine("(c) Mason Schmidgall 2016 - 2018. All rights reserved.\n");

			// Print Current Hangman image
			if (statusCode == 8)
			{
				Console.WriteLine(hip.getVictoryImage() + "\n");
			}
			else {
				Console.WriteLine(hip.getImage(hangman.getErrors()) + "\n");
			}

			// Print Current State
			Console.WriteLine(hangman.getState());
			Console.WriteLine("________________");

			// Print Letters Guessed
			Console.WriteLine("Guessed Letters:");
			Console.WriteLine(hangman.getLettersGuessed());

			// Print Errors
			Console.WriteLine("________________");
			Console.WriteLine("Errors Made: {0}\n", hangman.getErrors());

			// Print Status Message
			Console.ForegroundColor = getStatusColor(statusCode);
			Console.WriteLine(getStatusMessage(statusCode) + "\n");
			Console.ForegroundColor = defaultColor;

			// Print Idle Message Prompt
			if (isGameSuspended(statusCode))
			{
				if (statusCode == -1)
				{
					Console.WriteLine();
				}
				else {
					Console.WriteLine("Press Any Key to Continue. . .");
				}
			}
			else {
				Console.WriteLine("Type a Letter to guess. . .");
			}


		}

		/// <summary>
		/// Displaies the exception to the console.
		/// </summary>
		/// <param name="e">Exception to be displayed.</param>
		public static void displayException(Exception e)
		{
			var startColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			string errorString = e.Message;

			if (e.InnerException != null)
			{
				errorString += " " + e.InnerException.Message;
			}

			Console.Error.WriteLine(errorString);
			Console.ForegroundColor = startColor;
		}

		/// <summary>
		/// Gets the status message using the status code provided.
		/// </summary>
		/// <returns>The status message.</returns>
		/// <param name="statusCode">Status code.</param>
		public static string getStatusMessage(int statusCode)
		{
			switch (statusCode)
			{
				case 0: return "Awaiting Guess...";
				case 1: return "Correct! Guess again.";
				case 2: return "Nope, That's incorrect. Try Again.";
				case 3: return "Is that even a letter? Try Again.";
				case 4: return "That letter seems familiar... Try again.";
				case 8: return "You Win! You should buy a lottery ticket.";
				case 9: return "You lose. Better luck next time loser!";
				case -1: return "Are you sure you want to exit? Y/N";

				default: return "(default)"; // For Debugging purposes
			}
		}

		/// <summary>
		/// Determines if the game should be suspended.
		/// </summary>
		/// <returns><c>true</c>, if status code is a suspension code, <c>false</c> otherwise.</returns>
		/// <param name="statusCode">Status code.</param>
		public static bool isGameSuspended(int statusCode)
		{
			switch (statusCode)
			{
				case 8:	// Victory
				case 9:	// Loss
				case -1: return true;	// Quit

				default: return false;
			}
		}

		/// <summary>
		/// Determines what the status message's color should be based on the status code.
		/// </summary>
		/// <returns>The status color.</returns>
		/// <param name="statusCode">Status code.</param>
		public static ConsoleColor getStatusColor(int statusCode)
		{
			switch (statusCode)
			{
				case 0: return defaultColor;
				case 1:
				case 8: return ConsoleColor.Green;
				case 2:
				case 9: return ConsoleColor.Red;
				case 4:
				case 3: return ConsoleColor.Yellow;
				case -1: return ConsoleColor.DarkYellow;

				default: return ConsoleColor.Blue; // For Debugging purposes
			}
		}
	}

	/// <summary>
	/// An Object that represents the hangman game.
	/// </summary>
	class Hangman
	{
		// Instance variables
		private string word;
		private string state;
		private bool[] lettersGuessed;
		private int errors;
		private bool victory;

		// These variables may be deprecated
		public static char LOWER_CHARACTER_OFFSET = 'a';
		public static char UPPER_CHARACTER_OFFSET = 'A';

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hangman.Hangman"/> class.
		/// </summary>
		/// <param name="word">Word.</param>
		public Hangman (string word) 
		{
			this.word = word;
			lettersGuessed = fillArray (false, new bool[26]);
			updateState ();

			errors = 0;
			victory = false;
		}

		/// <summary>
		/// Gets the victory (You can figure it out).
		/// </summary>
		/// <returns><c>true</c>, if victory was gotten, <c>false</c> otherwise.</returns>
		public bool getVictory()
		{
			return victory;
		}

		/// <summary>
		/// Makes a guess with the given letter.
		/// </summary>
		/// <returns>The state after the guess has been made</returns>
		/// <param name="letter">Letter to guess. MUST BE A LETTER! (non-case-sensitive)</param>
		public string makeGuess (char letter) {

			if (!isInWord(letter)) errors++;

			setLetterGuessed (letter);

			updateState ();

			return state;
		}

		/// <summary>
		/// Gets the number of Errors made.
		/// </summary>
		/// <returns>The number of errors made.</returns>
		public int getErrors()
		{
			return errors;
		}

		/// <summary>
		/// Checks if the given guess is in the word or not.
		/// </summary>
		/// <returns>Whether or not the character was in the word.</returns>
		/// <param name="guess">Guess.</param>
		private bool isInWord(char guess)
		{
			foreach (char letter in word)
			{
				if (letter == Char.ToLower(guess) || letter == Char.ToUpper(guess))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Fills the given boolean array with the given value.
		/// </summary>
		/// <returns>The filled array.</returns>
		/// <param name="value">Value.</param>
		/// <param name="array">Array.</param>
		private static bool[] fillArray(bool value, bool[] array) 
		{
			for (int i = 0; i < array.Length; i++) {
				array [i] = value;
			}

			return array;
		}

		/// <summary>
		/// Updates the state string with any guessed letters revealed.
		/// </summary>
		private void updateState() {

			state = "";

			char[] wordArray = word.ToCharArray ();

			for (int i = 0; i < wordArray.Length; i++) {

				state += " ";

				if (lettersGuessed [getLetterIndex (wordArray [i])]) {
					state += wordArray [i];
				} else {
					state += "_";
				}
			}

			victory |= !state.Contains("_");
		}

		/// <summary>
		/// Gets the index of the given letter in the lettersGuessed Array.
		/// </summary>
		/// <returns>The letter's index.</returns>
		/// <param name="letter">Letter.</param>
		private static int getLetterIndex (char letter) {

			int position = letter - UPPER_CHARACTER_OFFSET;

			if (position > 25) {
				position = letter - LOWER_CHARACTER_OFFSET;
			}

			return position;
		}

		/// <summary>
		/// Sets the given letter to guessed.
		/// </summary>
		/// <param name="guess">Guess.</param>
		private void setLetterGuessed(char guess) {
			lettersGuessed [getLetterIndex (guess)] = true;
		}

		/// <summary>
		/// Determines if the given character is a letter.
		/// </summary>
		/// <returns>Whether or not it's a letter.</returns>
		/// <param name="character">Character.</param>
		public static bool isLetter(char character)
		{
			if (getLetterIndex(character) < 0 || getLetterIndex(character) > 25)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the letters guessed as a string
		/// </summary>
		/// <returns>The letters guessed.</returns>
		public string getLettersGuessed()
		{
			string letterString = "";

			for (int i = 0; i < lettersGuessed.Length; i++)
			{
				bool guessed = lettersGuessed[i];
				if (guessed)
				{
					letterString += " " + toLetter(i);
				}
			}

			return letterString;
		}

		/// <summary>
		/// Converts the given index to the letter it represents.
		/// </summary>
		/// <returns>The letter.</returns>
		/// <param name="index">Index.</param>
		public static char toLetter(int index)
		{
			return (char)(index + UPPER_CHARACTER_OFFSET);
		}

		/* Depending on Implementation, these may be replaced, modified or removed */
		public string getState (bool forceRefresh) {

			if (forceRefresh) {
				updateState ();
			}

			return state;
		}

		/// <summary>
		/// Gets the state.
		/// </summary>
		/// <returns>The state.</returns>
		public string getState () {
			return state;
		}
	}

	class HangmanImageFileProcessor : HangmanImageProcessor
	{
		// Instance varibles
		string[] images;
		int currentImage;
		readonly string fileLocation;

		public static string DEFAULT_LOCATION = "HangmanImages.hif";

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hangman.HangmanImageFileProcessor"/> class.
		/// </summary>
		/// <param name="fileLocation">File location.</param>
		public HangmanImageFileProcessor(string fileLocation)
		{
			this.fileLocation = fileLocation;
			readImages();
			currentImage = -1;
		}

		/// <summary>
		/// Gets the image using the imageLocation as the index.
		/// </summary>
		/// <returns>The image.</returns>
		/// <param name="imageLocation">Image location.</param>
		public string getImage(int imageLocation)
		{
			currentImage = imageLocation;
			return images[imageLocation];
		}
		/*	Method removed: Redundant in final implementation
		public string getNextImage()
		{
			currentImage++;
			return getImage(currentImage);
		}
		*/

		/// <summary>
		/// Gets the victory image (Last image in the array).
		/// </summary>
		/// <returns>The victory image.</returns>
		public string getVictoryImage()
		{
			return getImage(images.Length - 1);
		}

		/// <summary>
		/// Reads the images from the file.
		/// </summary>
	 	void readImages()
		{
			// Line read
			string line;

			try
			{
				// Scan the file to check format and get number of images
				int numImages = scanFile(fileLocation);

				// create a streamReader at the filelocation
				var file = new StreamReader(@fileLocation);
				if (numImages != 0)
				{
					// create a new string array to hold the images
					images = new string[numImages];

					// read to "@0" token to ignore any file header comments
					while (!(line = file.ReadLine()).Equals("@0")) { }

					// Read the images
					string token;
					for (int i = 1; i <= images.Length; i++)
					{
						// determine next image to look for
						if (i == numImages)
						{
							token = "@@@";
						}
						else if (i == numImages - 1)
						{
							token = "@@";
						}
						else {
							token = "@" + i;
						}

						string imageString = "";

						// read until the next token is reached
						while (!(line = file.ReadLine()).Equals(token))
						{
							imageString += line + "\n";
						}

						// save the image that was read
						images[i - 1] = imageString;
					}
				}

				// close the stream reader
				file.Close();
			}
			// catch the exceptions thrown by the scanfile method
			catch (UnexpectedEndOfFileException e)
			{
				throw new FileNotFormattedException("Image file was not formatted correctly:", e);
			}
			catch (UnxepectedTokenException e)
			{
				throw new FileNotFormattedException("Image file was not formatted correctly:", e);
			}
			catch (FileNotFormattedException e)
			{
				throw new FileNotFormattedException("Image file was not formatted correctly:", e);
			}
			catch (FileNotFoundException e)
			{
				throw e;
			}
			catch (IOException e)
			{
				throw e;
			}
		}

		/// <summary>
		/// Scans the file for formatting errors and counts the number of images.
		/// </summary>
		/// <returns>The file.</returns>
		/// <param name="fileLocation">File location.</param>
		private static int scanFile(string fileLocation)
		{
			// a bunch of self explainitory variables
			string text;
			int i = 0;
			bool lastImageReached = false;
			var file = new StreamReader(@fileLocation);

			// traverse the file
			while ((text = file.ReadLine()) != null)
			{
				// check if the line is a normal token
				if (isCharPlusNumber(text, '@'))   // The "@@" Token will be used before the victory image
				{
					// check if tokens are in right order
					if (!text.Equals("@" + i))
					{
						throw new FileNotFormattedException("Image tokens are in wrong order.");
					}

					i++;
				}
				// check if it's a victory token
				else if (text.Equals("@@"))
				{
					// check if another token appeared after the first victory image token
					if (lastImageReached)
					{
						// Another Image Entry was detected after the Victory Image
						// Allowing File processing to continue past this would cause an incorrect image to display at victory
						throw new UnxepectedTokenException("File contained another non-End-Of" +
						                                   "-File token after the \"@@\" (Victory Image) token.");
					}

					lastImageReached = true;  // lastImageReached is set true if text == "@@"

					i++;
				}
				else if (text.Equals("@@@"))	// The "@@@" Token means End Of File. Any lines after this will be ignored
				{
					if (lastImageReached)
					{
						file.Close();
						return i;
					}
					else
					{
						throw new UnxepectedTokenException("A \"@@@\" (EOF) token was found before a \"@@\" (Victory Image) token");
					}
				}


			}
			// End of file reached before "@@@" token was found.
			// Allowing File Processing to continue past this could cause 
			//	 lines that were intended as comments to be displayed as images
			throw new UnexpectedEndOfFileException("File ended before a \"@@@\" token was found.");
		}

		/// <summary>
		/// Gets the number images in the images array.
		/// </summary>
		/// <returns>The number images.</returns>
		public int getNumImages()
		{
			return images.Length;
		}

		/// <summary>
		/// Determines if the given text contains the given character plus any integer number.
		/// </summary>
		/// <returns>Whether or not it is the char plus a number</returns>
		/// <param name="text">String to check.</param>
		/// <param name="character">Character to check with.</param>
		private static bool isCharPlusNumber(string text, char character)
		{
			int value;
			if (text.Length > 0)
			{
				if (text[0] == '@')
				{
					string[] s = text.Split('@');
					return int.TryParse(s[1], out value);	// the value is ignored
				}
			}

			return false;
		}
	}

	public class HangmanDictionaryFileProcessor
	{
		// A couple self explainitory instance variables
		string[] words;
		readonly string fileLocation;

		public static string DEFAULT_LOCATION = "words.txt";

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Hangman.HangmanDictionaryFileProcessor"/> class.
		/// </summary>
		/// <param name="fileLocation">File location.</param>
		public HangmanDictionaryFileProcessor(string fileLocation)
		{
			this.fileLocation = fileLocation;
			readFile();
		}

		/// <summary>
		/// Returns a random word from the dictionary file
		/// </summary>
		/// <returns>The random word.</returns>
		public string getRandomWord()
		{
			var rdm = new Random();

			return words[rdm.Next(words.Length)];
		}

		/// <summary>
		/// Reads the file.
		/// </summary>
		private void readFile()
		{
			var file = new StreamReader(@fileLocation);

			words = new string[0];

			string text;

			while ((text = file.ReadLine()) != null)
			{
				// make sure the word will play nice with the hangman object
				if (checkStringLettersOnly(text)) {
					words = addElementToArray(text, words);
				}
			}

			file.Close();

			if (words.Length == 0)
			{
				throw new EmptyFileException("No words were found in the dictionary file.");	// figure it out
			}
		}

		/// <summary>
		/// Adds the element to the given array.
		/// </summary>
		/// <returns>The array with the added element.</returns>
		/// <param name="element">Element.</param>
		/// <param name="array">Array.</param>
		public static string[] addElementToArray(string element, string[] array)
		{

			// create an array with a size of one larger than the one provided
			string[] newArray = new string[array.Length + 1];

			// copy elements to the array
			for (int i = 0; i < array.Length; i++)
			{
				newArray[i] = array[i];
			}

			// add the given element to the last position of the new array
			newArray[newArray.Length - 1] = element;

			// return to sender
			return newArray;
		}

		/// <summary>
		/// Checks the string for whether or not it contains only Letters.
		/// </summary>
		/// <returns>True for Pass, False for fail.</returns>
		/// <param name="text">String to check.</param>
		public static bool checkStringLettersOnly(string text)
		{
			foreach (char character in text)
			{
				if (!(Char.IsLetter(character)))
				{
					return false;
				}
			}

			return true;
		}
	}

	class DefaultHangmanImageProcessor : HangmanImageProcessor
	{
		int numImages;

		public DefaultHangmanImageProcessor(int numImages)
		{
			this.numImages = numImages;
		}

		public string getImage(int imageLocation)
		{
			return "Errors: " + imageLocation;
		}

		public int getNumImages()
		{
			return numImages;
		}

		public string getVictoryImage()
		{
			return "You Win!";
		}


	}

	interface HangmanImageProcessor
	{
		string getImage(int imageLocation);

		string getVictoryImage();

		int getNumImages();
	}

	// Skeletons for exceptions below
	public class UnxepectedTokenException : Exception
	{
		public UnxepectedTokenException() { }

		public UnxepectedTokenException(string message): base(message) { }

		public UnxepectedTokenException(string message, Exception innerException) : base(message, innerException) { }
	}

	public class UnexpectedEndOfFileException : Exception
	{
		public UnexpectedEndOfFileException() { }

		public UnexpectedEndOfFileException(string message) : base(message) { }

		public UnexpectedEndOfFileException(string message, Exception innerException) : base(message, innerException) { }
	}

	public class FileNotFormattedException : Exception
	{
		public FileNotFormattedException() { }

		public FileNotFormattedException(string message) : base(message) { }

		public FileNotFormattedException(string message, Exception innerException) : base(message, innerException) { }
	}

	public class EmptyFileException : Exception
	{
		public EmptyFileException() { }

		public EmptyFileException(string message) : base(message) { }

		public EmptyFileException(string message, Exception innerException) : base(message, innerException) { }
	}
}
