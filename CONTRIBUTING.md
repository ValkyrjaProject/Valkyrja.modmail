## How to contribute

Clone the repository recursively to include the Core project:
* Fork this repository, and then clone it recursively to get the Core library as well: `git clone --recursive git@github.com:YOURUSERNAME/Valkyrja.modmail.git`
  * If you plan to contribute to the Core as well, fork it as well, and clone them both with `git clone git@github.com:YOURUSERNAME/Valkyrja.modmail.git && cd Valkyrja.modmail && git clone git@github.com:YOURUSERNAME/Valkyrja.coreLite.git Core`
* Nuke your nuget cache `dotnet nuget locals all --clear`

1. Create a new branch. This can be done easily on github.
  1. Naming convention: `<type of branch>-<name of your contribution>` where
    * `<type of branch>` is generally either `feature`, `improvement` or `fix` (similar to issue labels)
    * `<name of your contribution>` would e whatever your code is going to be about, where it should use camelCase. In case of an issue, just add the ticket number.
  2. Examples:
    * `feature-commandOverrides` (without an issue)
    * `improvement-123-youtubeNotifications` (for issue `#123`)
    * `fix-123` (for issue `#123`) _Please don't use **just** the number for bigger features, add some title to know what's that about without having to look it up._
2. Commit your code properly into your branch as you work on it.
  1. Recommended IDE to write your code (You can also refer to [fedoraloves.net](http://fedoraloves.net) for further information on C# in Fedora Linux.)
    * [Jetbrains Rider](https://www.jetbrains.com/rider) - Windows, Linux and Mac. Prefered choice and active contributors will receive a license from Rhea.
    * [Visual Studio Code](https://code.visualstudio.com) - Windows, Linux and Mac.
    * Standard Visual Studio is not recommended, however you can use it if you prefer. There are issues ;)
  2. Follow our naming conventions and code style guide below. (Set up your IDE for it...)
  3. Discuss your problems and ideas with our awesome dev team on Discord, to further improve them!
3. Test your code.
  1. [Jetbrains Rider](https://www.jetbrains.com/rider) can nicely build, debug and run both mono and netcore on both Windows and Linux. VS, VSCode, MonoDevelop or Xamarin are not recommended for debugging.
4. Submit PullRequest when you're done. This can be done easily on github. e.g. [1.](https://i.imgur.com/vF1uSMm.png) [2.](https://i.imgur.com/mbNvr3c.png)
  1. New features or improvements or any other large changes should go into the `dev` branch.
  2. Really tiny fixes and typos, or tiny improvements of a response message, etc, can go straight into `master`. If in doubt ask.
  3. If there is an issue for your PR, make sure to mention the `#number` in the title.

## Code style and Naming Conventions

Just a few guidelines about the code:

* If you're writing a new module, try to write summary for public and internal methods.
* Use PascalCase for member properties and `this` notation when accessing them. Treat internal as public, and protected as private. Treat constants as public as well, ie PascalCase.
* Always immediately initialise variables.
* Always explicitly declare whether methods are public or private. If they are async, this keyword should be second (or third in case of static methods `public static async Task ...`)
* Never return `void` with async methods. Return `Task` instead of `void`.
  ```cs
    
  public class BunnehClient<TUser>: IClient<TUser> where TUser: UserData, new()
  {
    public enum ConnectionState
    {
      None = 0,
      Bad,
      Good,
      Peachy
    }
    
    internal const int LoopLimit = 60;
    
    public ConnectionState State = ConnectionState.None;
    
    internal int LoopCount{ get; private set; } = 0;
  
		
    /// <summary> This is blocking call that will await til the connection is peachy.
    /// Returns true if the operation was canceled, false otherwise. </summary>
    public async Task<bool> AwaitConnection<TUser>(TUser user) where TUser: UserData, new()
    {
      while( this.State != ConnectionState.Peachy )
      {
        if( this.LoopCount++ >= this.LoopLimit )
          return true;

        await Task.Delay(1000);
      }
  
      await user.SendMessageAsync("You have been connected!");
      return false;
    }
  }

  ```

Please try to set-up your IDE to handle this for you:

* Use tabs, do not expand to spaces. (Yes we're that old.)
* **Always** use explicit types. **Do Not Use `var`!**
* Set the IDE to remove trailing whitespace, it triggers OCD...
* Default VS-style will try to format your code in rather weird way that is a little irational. Please follow the above displayed format: `if( something )`. (Note that the VS style would place spaces for if statement this way: `if (something)`)


