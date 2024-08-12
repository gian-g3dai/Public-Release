This extension allows you to utilize Unakin’s functionality directly within Visual Studio.

# Tool

Please note that this package is optimized for C#, and some functionalities may not work for other languages.

# "Unakin Chat" tool window 🛠
To access the main tool window, right-click in your IDE and select "Unakin Chat":

![image](https://github.com/unakin/public-images/blob/main/main.png)

In this tool window, you can ask questions to Unakin and receive answers within the window:

![image](https://github.com/unakin/public-images/blob/main/Chat__1.png)

The answers will be stored in the "Chat History," which can be accessed by clicking the corresponding button at the top of the window.

If desired, you can select a directory from the button "Directory" and enable the "Add Context" toggle, providing contextual information from your directory to the request. Please note that this feature is only available for standard chats.

There are four special commands that can be accessed by entering "//" in the chat; these are:

//Search in Code
//Project Summary
//Automated Testing
//Data Generation
![image](https://github.com/unakin/public-images/blob/main/list.png)


To use 'Search in Code,' you need to assign a working directory in the 'Directory' section, accessed by clicking the button next to 'Chat History' at the top of the window, and a project name in the settings, accessible through the icon on the top right. Once you've selected the directory and assigned the name (use only standard alphabetical characters for the name), you can inquire about the location of a specific piece of code within your folder, based on a prompt.

Project Summary and Automated Testing will display a pop-up where you can select a project folder. They do not require a prompt. Project Summary will summarize the project for you, while Automated Testing will generate unit tests for all the scripts in the folder.

Data Generation will open a window where you can specify the data you require and generate synthetic data accordingly. You can use this to create datasets - for instance, you could create a list of 20 sword names with 20 accompanying characteristics. You can save the dataset in CSV format.

# Features in the code editor 👩‍💻

If you wish to interact with your code, highlight the relevant snippet, right-click, and select one of the following commands:

Complete: Begin writing a method, select it, and request completion.
Add Tests: Generate unit tests for the selected method.
Find Bugs: Identify bugs in the selected code.
Optimize: Optimize the selected code.
Explain: Provide an explanation of the selected code.
Add Comments: Add comments to the selected code.
Add Summary: Add a summary for C# methods.
Translate: Replace the selected text with its English translation.
Cancel: Cancel any command requests that are being processed or awaited.
Unakin Chat: Opens the window where you can chat with Unakin.
Tutorial: Displays a short tutorial with key points.

Unakin will process your request, and the generated code will be shown directly in the Unakin Chat window. You will be able to copy it, replace the old code with the generated one, or get a zoomed view. See the explain command in action below:

![image](https://github.com/unakin/public-images/blob/main/IDE-Commands.png)

# Agent Hub 🚀

The Agent Hub serves as a framework where you can either construct a workflow or utilize an autonomous one. When constructing your workflow, you have the ability to define agents (think of them as autonomous coding partners) and collaborate seamlessly. The framework includes three preconfigured agents designed to aid in creating, adding comments to, and optimizing your script. Alternatively, you can establish your own agents by clicking the 'Add Agent' button—ensure the active toggle is enabled. Once defined, you can arrange them in the desired logical sequence using drag and drop functionality. These agents will then collectively address your requests during your chats. To access the Hub, simply toggle the switch to "On":

In the autonomous workflow, agents will respond to your complex requests by breaking them down into individual steps, before actioning these steps to achieve a broader objective. For instance, if providing a prompt such as "Create a Shooter game in Unity", you'll receive multiple scripts and instructions. Sit back and allow the agents to handle the workload.

![image](https://github.com/unakin/public-images/blob/main/agents.png)

Additionally, you can access the Local Workflow special command by entering "//" in the chat. This feature allows you to select a local folder and apply the agent steps to your local scripts, enabling automatic commenting or optimization.

# Authentication 🔑

Advanced settings can be accessed at Tools->Options->Visual Unakin Studio.

