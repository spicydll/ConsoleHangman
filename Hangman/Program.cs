using System;
using System.IO;
using System.Threading;

namespace Hangman
{
	class MainClass
	{
		static string version = "1.0";
		static readonly ConsoleColor defaultColor = Console.ForegroundColor;
		Hangman hangman;
		HangmanImageFileProcessor hifp;
		HangmanDictionaryFileProcessor hdfp;
		bool firstPass;
		bool customWord;


		private MainClass(bool customWord, HangmanImageFileProcessor hifp, HangmanDictionaryFileProcessor hdfp)
		{
			this.hifp = hifp;
			this.hdfp = hdfp;
			hangman = new Hangman(hdfp.getRandomWord());
			this.customWord = customWord;
			firstPass = true;
		}

		private MainClass(Hangman hangman, HangmanImageFileProcessor hifp)
		{
			this.hangman = hangman;
			this.hifp = hifp;
			hdfp = null;
			customWord = false;
			firstPass = true;
		}

		public static void Main (string[] args)
		{
			try
			{
				var mc = new MainClass(new Hangman("Foo"), new HangmanImageFileProcessor("HangmanImages.txt"));

				mc.runHangman();
			}
			catch (FileNotFormattedException e)
			{
				displayException(e);
			}
			catch (FileNotFoundException e)
			{
				displayException(e);
			}
			catch (IOException e)
			{
				displayException(e);
			}
		}


		public static MainClass handleArguments(string[] args)
		{
			Hangman hangman = null;
			HangmanImageFileProcessor hifp = null;
			HangmanDictionaryFileProcessor hdfp = null;
			bool customWord = false;
			int currentArg = 0;

			while (currentArg < args.Length)
			{
				switch (args[currentArg])
				{
					case "-i":
					case "-imageFile":
						{
							currentArg++;

							if (currentArg < args.Length)
							{
								hifp = new HangmanImageFileProcessor(args[currentArg]);
							}
							else
							{
								throw new FormatException("Image File Location was not specified.");
							}
							break;
						}

					case "-d":
					case "-dictionaryFile":
						{
							currentArg++;

							if (currentArg < args.Length)
							{
								hdfp = new HangmanDictionaryFileProcessor(args[currentArg]);
							}
							else {
								throw new FormatException("Dictionary File location was not specified.");
							}
							break;
						}

					case "-W":
					case "-Word":
						{
							customWord = true;
							break;
						}

					default:
						{
							if (HangmanDictionaryFileProcessor.checkStringLettersOnly(args[currentArg]))
							{
								hangman = new Hangman(args[currentArg]);
							}
							else {
								throw new FormatException("Explicit Word argument contained illegal character(s)");
							}

							break;
						}
				}

				currentArg++;
			}

			if (hifp == null)
			{
				hifp = new HangmanImageFileProcessor(HangmanImageFileProcessor.DEFAULT_LOCATION);
			}

			if (!customWord)
			{
				if (hdfp == null)
				{
					hdfp = new HangmanDictionaryFileProcessor(HangmanDictionaryFileProcessor.DEFAULT_LOCATION);
				}
			}

			return null;	// Temporary For compiler sake
		}

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
					return;	// Exits the program
				}

				runHangman();   // Runs this method again.
				return;	// Makes sure no further code is executed after recursion is complete
			}

			Console.ReadKey(true);
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

				if (hangman.getErrors() == hifp.getNumImages() - 2)
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
			Console.WriteLine("Console Hangman v{0} ([ESC] to exit)\n", version);

			// Print Current Hangman image
			if (statusCode == 8)
			{
				Console.WriteLine(hifp.getVictoryImage() + "\n");
			}
			else {
				Console.WriteLine(hifp.getImage(hangman.getErrors()) + "\n");
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

		public static bool isGameSuspended(int statusCode)
		{
			switch (statusCode)
			{
				case 8:
				case 9:
				case -1: return true;

				default: return false;
			}
		}

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

	class Hangman
	{
		private string word;
		private string state;
		private bool[] lettersGuessed;
		private int errors;
		private bool victory;

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

	class HangmanImageFileProcessor
	{
		string[] images;
		int currentImage;
		readonly string fileLocation;

		public static string DEFAULT_LOCATION = "HangmanImages.hif";

		public HangmanImageFileProcessor(string fileLocation)
		{
			this.fileLocation = fileLocation;
			readImages();
			currentImage = -1;
		}

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

		public string getVictoryImage()
		{
			return getImage(images.Length - 1);
		}

	 	void readImages()
		{
			string line;

			try
			{
				int numImages = scanFile(fileLocation);

				var file = new StreamReader(@fileLocation);
				if (numImages != 0)
				{

					images = new string[numImages];

					while (!(line = file.ReadLine()).Equals("@0")) { }

					string token;
					for (int i = 1; i <= images.Length; i++)
					{
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

						while (!(line = file.ReadLine()).Equals(token))
						{
							imageString += line + "\n";
						}

						images[i - 1] = imageString;
					}
				}

				file.Close();
			}
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

		private static int scanFile(string fileLocation)
		{
			string text;
			int i = 0;
			bool lastImageReached = false;
			var file = new StreamReader(@fileLocation);

			while ((text = file.ReadLine()) != null)
			{
				if (isCharPlusNumber(text, '@'))   // The "@@" Token will be used before the victory image
				{
					if (!text.Equals("@" + i))
					{
						throw new FileNotFormattedException("Image tokens are in wrong order.");
					}

					i++;
				}
				else if (text.Equals("@@"))
				{
					if (lastImageReached)
					{
						// Another Image Entry was detected after the Victory Image
						// Allowing File processing to continue past this would cause an incorrect image to display at victory
						throw new UnxepectedTokenException("File contained another non-End-Of" +
														   "-File token after the \"@@\" token.");
					}

					lastImageReached = true;  // just means lastImageReached is set true if text == "@@"

					i++;
				}
				else if (text.Equals("@@@"))	// The "@@@" Token means End Of File. Any lines after this will be ignored
				{
					file.Close();
					return i;
				}


			}
			// End of file reached before "@@@" token was found.
			// Allowing File Processing to continue past this could cause 
			//	 lines that were intended as comments to be displayed as images
			throw new UnexpectedEndOfFileException("File ended before a \"@@@\" token was found.");
		}

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
					return int.TryParse(s[1], out value);
				}
			}

			return false;
		}
	}

	public class HangmanDictionaryFileProcessor
	{
		string[] words;
		readonly string fileLocation;

		public static string DEFAULT_LOCATION = "words.txt";

		public HangmanDictionaryFileProcessor(string fileLocation)
		{
			this.fileLocation = fileLocation;
			readFile();
		}

		public string getRandomWord()
		{
			Random rdm = new Random();

			return words[rdm.Next(words.Length)];
		}

		private void readFile()
		{
			var file = new StreamReader(@fileLocation);

			words = new string[0];

			string text;

			while ((text = file.ReadLine()) != null)
			{
				if (checkStringLettersOnly(text)) {
					words = addElementToArray(text, words);
				}
			}

			file.Close();

			if (words.Length == 0)
			{
				throw new EmptyFileException("No words were found in the dictionary file.");
			}
		}

		public static string[] addElementToArray(string element, string[] array)
		{
			string[] newArray = new string[array.Length + 1];

			for (int i = 0; i < array.Length; i++)
			{
				newArray[i] = array[i];
			}

			newArray[newArray.Length - 1] = element;

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
