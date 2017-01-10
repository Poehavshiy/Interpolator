using Microsoft.Research.Oslo;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveODE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPH_2D {
    public class ViewModel {
        private ScatterSeries Ro;
        private ScatterSeries V;
        private ScatterSeries P;
        private ScatterSeries E;
        private ScatterSeries colorSer2D;
        double Ro0, P0, E0;

        public VMPropRx<PlotModel,SolPoint> Model1Rx { get; private set; }
        public int DrawState { get; set; } = 1;
        public int WichGraph { get; set; } = 0;
        Sph2D _curr4Draw;
        private LinearColorAxis colorAxis;

        public VMPropRx<List<SolPoint>,SolPoint> SolPointList { get; private set; }

        public ViewModel(Sph2D curr) {
            _curr4Draw = MainWindow.GetTest();
            _curr4Draw.Rebuild();

            Fill0s(curr);

            Model1Rx = new VMPropRx<PlotModel,SolPoint>(
                () => {
                    var Model1 = GetNewModel("params","X","p,Ro,V");
                    P = new ScatterSeries() {
                        Title = "P",
                        MarkerType = MarkerType.Triangle,
                        MarkerSize = 2,
                        //ColorAxisKey = colorAxis.Key,
                        
                    };
                    Model1.Series.Add(P);

                    Ro = new ScatterSeries() {
                        Title = "Ro",
                        MarkerType = MarkerType.Diamond,
                        MarkerSize = 2,
                        //ColorAxisKey = colorAxis.Key
                    };
                    Model1.Series.Add(Ro);

                    V = new ScatterSeries() {
                        Title = "V",
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 2,
                       // ColorAxisKey = colorAxis.Key
                    };
                    Model1.Series.Add(V);

                    E = new ScatterSeries() {
                        Title = "E",
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 2,
                       // ColorAxisKey = colorAxis.Key
                    };
                    Model1.Series.Add(E);

                    colorSer2D = new ScatterSeries() {
                        Title = "Color",
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 2,
                        ColorAxisKey = colorAxis.Key
                    };
                    Model1.Series.Add(colorSer2D);

                    return Model1;
                },
                (sp,pm) => {
                    _curr4Draw.SynchMeTo(sp);
                    switch(DrawState) {
                        case 1: {
                            Draw2D(sp.T,pm,WichGraph);
                            break;
                        }
                        default:
                            Draw(sp.T,pm);
                        break;
                    }
                    
                    return pm;
                });

            SolPointList = new VMPropRx<List<SolPoint>,SolPoint>(
                () => new List<SolPoint>(),
                (sp,lst) => {
                    lst.Add(sp);
                    return lst;
                });

        }

        public void Fill0s(Sph2D curr) {
            Ro0 = curr.AllParticles.Cast<IGasParticleVer3>().Max(p => p.Ro);
            P0 = curr.AllParticles.Cast<IGasParticleVer3>().Max(p => p.P);
            E0 = curr.AllParticles.Cast<IGasParticleVer3>().Max(p => p.E);
        }

        public void Draw(double t,PlotModel pm) {
            //pm.Axes.Remove(colorAxis);
            Ro.Points.Clear();
            Ro.MarkerFill = OxyColors.DarkOrange;
            V.Points.Clear();
            V.MarkerFill = OxyColors.Red;
            P.Points.Clear();
            P.MarkerFill = OxyColors.Green;
            E.Points.Clear();
            E.MarkerFill = OxyColors.Blue;
            colorSer2D.Points.Clear();
            foreach(var p in _curr4Draw.AllParticles.Cast<IGasParticleVer3>()) {
                Ro.Points.Add(new ScatterPoint(p.X,p.Ro / Ro0,value: p.Ro / Ro0));
                V.Points.Add(new ScatterPoint(p.X,p.VelVec2D.GetLength() / p.C,value: p.VelVec2D.GetLength() / p.C));
                P.Points.Add(new ScatterPoint(p.X,p.P / P0,value: p.P / P0));
                E.Points.Add(new ScatterPoint(p.X,p.E / E0,value: p.E / E0));
            }
            pm.Title = $"{t:0.##########} s,  RoMax = {_curr4Draw.Particles.Cast<IGasParticleVer3>().Max(p => p.Ro):0.###},  Pmax = {_curr4Draw.Particles.Cast<IGasParticleVer3>().Max(p => p.P):0.###}";
            pm.InvalidatePlot(true);
        }

        public void Draw2D(double t, PlotModel pm, int ind) {
           // pm.Axes.Add(colorAxis);
            Ro.Points.Clear();

            V.Points.Clear();
            P.Points.Clear();
            
            E.Points.Clear();

            colorSer2D.Points.Clear();

            switch(ind) {
                case 0:
                    foreach(var p in _curr4Draw.AllParticles.Cast<IGasParticleVer3>()) {
                    colorSer2D.Points.Add(new ScatterPoint(p.X,p.Y,value: p.P / P0));
                        
                    }
                    break;
                case 1:
                foreach(var p in _curr4Draw.AllParticles.Cast<IGasParticleVer3>()) {
                    colorSer2D.Points.Add(new ScatterPoint(p.X,p.Y,value: p.Ro / Ro0));
                    
                }
                break;
                case 2:
                foreach(var p in _curr4Draw.AllParticles.Cast<IGasParticleVer3>()) {

                    colorSer2D.Points.Add(new ScatterPoint(p.X,p.Y,value: p.VelVec2D.GetLength() / p.C));
                   

                }
                break;
                default:               
                foreach(var p in _curr4Draw.AllParticles.Cast<IGasParticleVer3>()) {

                    colorSer2D.Points.Add(new ScatterPoint(p.X,p.Y,value: p.E / E0));
                    
                }
                break;
                
            }
            pm.Title = $"{t:0.##########} s,  RoMax = {_curr4Draw.Particles.Cast<IGasParticleVer3>().Max(p => p.Ro):0.###},  Pmax = {_curr4Draw.Particles.Cast<IGasParticleVer3>().Max(p => p.P):0.###}";
            pm.InvalidatePlot(true);
        }

        public PlotModel GetNewModel(string title = "",string xname = "",string yname = "") {
            var m = new PlotModel { Title = title };
            var linearAxis1 = new LinearAxis();
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MaximumPadding = 0;
            linearAxis1.MinimumPadding = 0;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            linearAxis1.Position = AxisPosition.Bottom;
            linearAxis1.Title = xname;
            m.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            linearAxis2.MaximumPadding = 0;
            linearAxis2.MinimumPadding = 0;
            linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            linearAxis2.Title = yname;
            m.Axes.Add(linearAxis2);

            colorAxis = new LinearColorAxis {
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Jet(200),
                Minimum = 0,
                Maximum = 1
            };
            m.Axes.Add(colorAxis);

            return m;
        }
    }
}
