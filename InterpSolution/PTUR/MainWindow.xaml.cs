using Microsoft.Research.Oslo;
using Microsoft.Win32;
using ReactiveODE;
using SimpleIntegrator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PTUR {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private PTURDyn pr;
        IEquasionController controller;
        private Microsoft.Research.Oslo.Vector v0;

        public ViewModelPTUR vm { get; set; }

        public MainWindow() {
            vm = new ViewModelPTUR();
            DataContext = vm;
            InitializeComponent();

            var sol = new PTURDyn(2000,800,1.7,11,PTURDyn.TargetPosition1);

            initObs(sol);//(0.001875+0.0075) * 0.5


            var trackbarch = Observable.FromEventPattern<RoutedPropertyChangedEventArgs<double>>(slider,"ValueChanged").Select(i => {
                int newVal = (int)i.EventArgs.NewValue;
                int index = newVal < vm.SolPointList.Value.Count ? newVal : vm.SolPointList.Value.Count - 1;
                return index;
            }).Publish();

            trackbarch.Subscribe(i => {
                if(i < 0)
                    return;
                vm.Model1Rx.Update(vm.SolPointList.Value[i]);
            });
            trackbarch.Connect();



        }



        private void initObs(PTURDyn calc) {
            pr = calc;
            pr.V_val = 50;
            v0 = pr.Rebuild(pr.TimeSynch);
            var dt = 0.0001;
            var sol = Ode.RK45(pr.TimeSynch,v0,pr.f,dt).WithStepRx(0.001,out controller).StartWith(new SolPoint(pr.TimeSynch,v0)).Publish();
            controller.Pause();

            sol.ObserveOnDispatcher().Subscribe(sp => {
                vm.SolPointList.Update(sp);
                slider.Maximum = (double)(vm.SolPointList.Value.Count > 0 ? vm.SolPointList.Value.Count : 0);
            });
            sol.Connect();
        }



        private void button_Click_1(object sender,RoutedEventArgs e) {
            controller.Paused = !controller.Paused;
            string txt = controller.Paused ? "Paused" : "Playing";
            button.Content = txt;

        }

        private void button_Save_Click_1(object sender,RoutedEventArgs e) {
            controller.Pause();
            button.Content = "Paused";
            var unit4save = new PTURDyn();
            unit4save.Rebuild();

            int newVal = (int)slider.Value;
            int index = newVal < vm.SolPointList.Value.Count ? newVal : vm.SolPointList.Value.Count - 1;
            if(index < 0)
                return;
            unit4save.SynchMeTo(vm.SolPointList.Value[index]);
            var sd = new SaveFileDialog() {
                Filter = "XML Files|*.xml",
                FileName = "sph1D"
            };
            if(sd.ShowDialog() == true) {
                var sw = new StreamWriter(sd.FileName);
                unit4save.Serialize(sw);
                sw.Close();
            }


        }

        private void button_Copy1_Click_1(object sender,RoutedEventArgs e) {
            controller.Pause();
            button.Content = "Paused";
            var unit4load = new PTURDyn();
            unit4load.Rebuild();
            var sd = new OpenFileDialog() {
                Filter = "XML Files|*.xml",
                FileName = "sph1D"
            };
            if(sd.ShowDialog() == true) {
                var sr = new StreamReader(sd.FileName);
                unit4load.Deserialize(sr);
                sr.Close();

                controller.Cancel();
                vm.SolPointList.Value.Clear();
                initObs(unit4load);



            }

        }

    }
}
