using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EdgeJs;

namespace NodeSchool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool codeFocused;

        public MainWindow()
        {
            InitializeComponent();

            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            Title += " v" + version;
        }

        private void Code_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!codeFocused)
            {
                // Clear out the default text for now
                Code.Text = "";
                codeFocused = true;
            }
        }

        private void Code_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Code.Text))
            {
                Code.Text = "Type code here...";
                codeFocused = false;
            }
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            ConsoleOutput.Text += "\n> Executing code...\n";
            var script = ComposeScript(Code.Text);
            Task.Run(() => Run(script)).Wait();
        }

        private async void Run(string script)
        {
            var consoleLog = (Func<object, Task<object>>)(async (message) =>
            {
                var str = Encoding.UTF8.GetString((byte[])message);
                ConsoleOutput.Dispatcher.BeginInvoke((Action) (() => ConsoleOutput.Text += str));
                return null;
            });

            var runFunc = Edge.Func(script);

            await runFunc(new {
                log = consoleLog
            });

            ConsoleOutput.Dispatcher.BeginInvoke((Action) (() => ConsoleOutput.Text += "\n"));
        }

        private string ComposeScript(string code)
        {
            const string template = @"
                return function (options, done) {
                    // Windows GUI Applications mess up the STDIN/STDOUT streams to the point
                    // where Node will get all sorts of mad. What we're doing here is telling
                    // Node to use our MagicStream which will basically send output up to our
                    // handler to execute.
                    (function () {
                        var stream = require('stream');
                        var MagicStream = function (o) {
                            stream.Writable.call(this);
                            this._write = function (c, e, cb) {
                                options.log(c); 
                                cb && cb();
                            };
                        }
                        require('util').inherits(MagicStream, stream.Writable);
                        var nullStream = new MagicStream();
                        process.__defineGetter__('stdout', function () { return nullStream; });
                        process.__defineGetter__('stderr', function () { return nullStream; });
                    })();
                    
                    {{CODE}}

                    done();
                }
            ";

            var script = template.Replace("{{CODE}}", code);
            return script;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ConsoleOutput.Text = "";
        }
    }
}
