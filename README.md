# SimpleScriptableObjectsDBWindow
**This is a Unity plugin to view simple scriptable objects in a table window with filters.**

This plugin is meant to be used in project with scriptable objects with many instances, such as card games.

It uses Reflection in order to create it askying you the smallest possible effort.

It will allow you to see your scriptable objects in a table and filter them in order to find them and work on them quickly.

Example:
![Screen0](https://user-images.githubusercontent.com/28757409/195982882-64fa7f8b-01e0-4ca4-a783-f3b736b1874f.png)

Example with filters:
![immagine](https://user-images.githubusercontent.com/28757409/195983994-4eefe852-fb79-49ee-a09d-0ea4142030b4.png)

Example with search:
![Screen2](https://user-images.githubusercontent.com/28757409/195983087-e94bda32-c0da-429e-8c2b-8c41d8c8bcf3.png)

## Usage
In order to use the plugin you need to:
* Put your scriptable objects in the **Resources** folder (or a subfolder of Resources)
* Create a class that:
    1. Uses **UnityEditor**
    2. Derives from **SimpleScriptableObjectDBWindow**
    3. Implements a funtion callable from the window menu to show the window calling the SimpleScriptableObjectDBWindow **Setup** function passing your ScriptableObject Type
    4. **(Optional)** Override width of each element type calling **OverrideWidths**
    
    You can see an example below:
    ![Screen3](https://user-images.githubusercontent.com/28757409/195983572-ccf02adf-bb7d-4833-89a7-ced90cf79f29.png)
    
Then you can find your window in the menu:

![immagine](https://user-images.githubusercontent.com/28757409/195983775-f67733cc-4e4f-473a-bacc-e5cce184f963.png)


## Examples
You can see all the above examles in the **SimpleScriptableObjectDBExample** folder.

## Not supported Types
Currently **structs** and **Arrays** are not supported.
