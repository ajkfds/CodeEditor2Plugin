using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CodeEditor2Plugin
{
    /*
    This interface is used to register/initialize a plugin for CodeEditor2.
    Since individual features are implemented by adding callbacks to each object in CodeEditor from the plugin side,
    we do not implement any functionality here other than registration/initialization.   
     */
    public interface IPlugin
    {
        // called first on bootup CodeEditor2. Register Plugin to CodeEditor2
        // return true if you complete register this plugin
        bool Register();

        // called after loading all Projects
        bool Initialize();

        // plugin ID. must be unique in all used Plugins
        string Id { get; }
    }
}
