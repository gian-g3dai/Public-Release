using System;
using System.Collections.Generic;
using System.Text;
using Unakin.Utils;

namespace UnakinShared.Enums
{
    enum CommandType
    {
        Code = 0,
        Request = 1
    }

    enum ChatType
    {
        Chat = 0,
        Agents = 1,
        IDE = 2,
        SemanticSearch = 3,
        ProjectSummary = Constants.PROJECTSUMMARY_ID,
        AutomatedTesting = Constants.AUTOMATEDTESTING_ID,
        AutonomousAgent= Constants.AUTONOMOUSAGENT_ID,
        DataGeneration = Constants.DATAGEN_ID
    }
}
