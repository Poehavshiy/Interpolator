using Microsoft.Research.Oslo;
using ReactiveODE;
using System;
using Microsoft.Research.Oslo;
using System.Collections.Generic;
using System.Linq;
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
using System.Reactive.Linq;
using Microsoft.Win32;
using System.IO;
using SimpleIntegrator;
using System.Threading;

namespace SPH_2D {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Sph2D pr;
        private IObservable<SolPoint> sol;
        IEquasionController controller;
        private Microsoft.Research.Oslo.Vector v0;
        public ViewModel vm { get; set; }

        public MainWindow() {
            pr = GetTest();
            vm = new ViewModel(pr);
            vm.Model1Rx.Value.PlotType = OxyPlot.PlotType.Cartesian;
            DataContext = vm;
            InitializeComponent();



            //(0.001875+0.0075) * 0.5
            initObs(pr);
            vm.DrawState = 1;
            vm.Model1Rx.Update(new SolPoint(pr.TimeSynch,pr.Rebuild()));

            var trackbarch = Observable.FromEventPattern<RoutedPropertyChangedEventArgs<double>>(slider,"ValueChanged").Select(i=>(int)i.EventArgs.NewValue);
            var rb0 = Observable.FromEventPattern<RoutedEventArgs>(radioButton,"Checked").Select(e => 0);
            var rb1 = Observable.FromEventPattern<RoutedEventArgs>(radioButton_Copy,"Checked").Select(e => 1);
            var grType = rb0.Merge(rb1).StartWith(0);
            

            var rb10 = Observable.FromEventPattern<RoutedEventArgs>(radioButton_Copy1,"Checked").Select(e => 0);
            var rb11 = Observable.FromEventPattern<RoutedEventArgs>(radioButton_Copy2,"Checked").Select(e => 1);
            var rb12 = Observable.FromEventPattern<RoutedEventArgs>(radioButton_Copy3,"Checked").Select(e => 2);
            var rb13 = Observable.FromEventPattern<RoutedEventArgs>(radioButton_Copy4,"Checked").Select(e => 3);
            var wichGraph = rb10.Merge(rb11).Merge(rb12).Merge(rb13).StartWith(0);

            var all = trackbarch.CombineLatest(grType,wichGraph,(i,gT,wG) => new Tuple<int,int,int>(i,gT,wG));

            all.
            Subscribe(t => {
                vm.DrawState = t.Item2;
                vm.WichGraph = t.Item3;

                redrawVm(t.Item1);
            });
        }

        private void initObs(Sph2D calc) {
            pr = calc;
            v0 = pr.Rebuild(pr.TimeSynch);
            //var dt = 0.0000001;
            var dt = 1000 * 1E-6;
            SPH2D_Ver3.dt = dt;
            sol = Ode.RK45(pr.TimeSynch,v0,pr.f,dt).WithStepRx(dt * 10,out controller).StartWith(new SolPoint(pr.TimeSynch,v0));
            //sol = SPH2D_Ver3.CoolIntegration(calc as Sph2D_improoveIntegr).WithStepRx(dt * 100,out controller).StartWith(new SolPoint(pr.TimeSynch,v0));
            controller.Pause();

            sol.ObserveOnDispatcher().Subscribe(sp => {
                vm.SolPointList.Update(sp);
                slider.Maximum = (double)(vm.SolPointList.Value.Count > 0 ? vm.SolPointList.Value.Count : 0);
            });
        }


        private void button_Click(object sender,RoutedEventArgs e) {
            controller.Paused = !controller.Paused;
            string txt = controller.Paused ? "Paused" : "Playing";
            button.Content = txt;
            
        }

        public static Sph2D GetTest() {
            //string real = "i1.txt";
            //string bound = "b1.txt";
            //return new My_Sph2D(real,bound);


            SPH2D_Ver3.h_default = 1d /50;
            SPH2D_Ver3.k = 3;
            SPH2D_Ver3.options = GasParticleOptions.DiffRoDiffE();
            //return SPH2D_Ver3.TestTruba(1,0.2,0.5,1,1,0.1,0.125,SPH2D_Ver3.h_default/2);
            //return SPH2D_Ver3.TestGasBall(1,1,0);
            //return new Sph2D_improoveIntegr(SPH2D_Ver3.TestTrubaParticles(1,0.2,0.5,3E4,1500,1E4,1200,SPH2D_Ver3.h_default));
            return new Sph2D_improoveIntegr(SPH2D_Ver3.TestGasBallParticles(1,1,0));

        }

        private void button_Save_Click(object sender,RoutedEventArgs e) {
            controller.Pause();
            button.Content = "Paused";
            var unit4save = GetTest();
            unit4save.Rebuild();

            int newVal = (int)slider.Value;
            int index = newVal < vm.SolPointList.Value.Count ? newVal : vm.SolPointList.Value.Count - 1;
            if(index < 0)
                return;
            unit4save.SynchMeTo(vm.SolPointList.Value[index]);
            var sd = new SaveFileDialog() {
                Filter = "XML Files|*.xml",
                FileName = "sph2D"
            };
            if(sd.ShowDialog() == true) {
                var sw = new StreamWriter(sd.FileName);
                unit4save.Serialize(sw);
                sw.Close();
            }


        }

        private void button_Copy1_Click(object sender,RoutedEventArgs e) {
            controller.Pause();
            button.Content = "Paused";
            var unit4load = GetTest();
            unit4load.Rebuild();
            var sd = new OpenFileDialog() {
                Filter = "XML Files|*.xml",
                FileName = "sph2D"
            };
            if(sd.ShowDialog() == true) {
                var sr = new StreamReader(sd.FileName);
                unit4load.Deserialize(sr);
                sr.Close();

                controller.Cancel();
                vm.SolPointList.Value.Clear();
                initObs(unit4load);
                vm.Model1Rx.Update(new SolPoint(pr.TimeSynch,pr.Rebuild()));


            }

        }

        void redrawVm(int newVal) {
            int index = newVal < vm.SolPointList.Value.Count ? newVal : vm.SolPointList.Value.Count - 1;
            if(index < 0)
                return;
            vm.Model1Rx.Update(vm.SolPointList.Value[index]);
        }
    }
}
