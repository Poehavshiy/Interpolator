using Microsoft.Research.Oslo;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveODE;
using Sharp3D.Math.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTUR {
    public class ViewModelPTUR {
        private LineSeries Pos;
        private LineSeries Trg;

        public VMPropRx<PlotModel,SolPoint> Model1Rx { get; private set; }
        public VMPropRx<List<SolPoint>,SolPoint> SolPointList { get; private set; }
        PTURDyn _curr4Draw;


        public ViewModelPTUR() {
            _curr4Draw = new PTURDyn();
            _curr4Draw.Rebuild();
            Model1Rx = new VMPropRx<PlotModel,SolPoint>(() => {
                var Model1 = GetNewModel("params","X","Y");
                Pos = new LineSeries() {
                    Title = "Traect",
                    // StrokeThickness = 2,
                    Color = OxyColors.Green
                };

                Model1.Series.Add(Pos);

                Trg = new LineSeries() {
                    Title = "Trg",
                    // StrokeThickness = 2,
                    Color = OxyColors.Red
                };
                Model1.Series.Add(Trg);
                return Model1;

            },
            (sp,pm) => {
                _curr4Draw.SynchMeTo(sp);
                Draw(sp.T,pm);
                return pm;
            }
            );

            SolPointList = new VMPropRx<List<SolPoint>,SolPoint>(
                () => new List<SolPoint>(),
                (sp,lst) => {
                    lst.Add(sp);
                    return lst;
                });


        }



        public void Draw(double t,PlotModel pm) {
            Pos.Points.Clear();
            Trg.Points.Clear();

            _curr4Draw.SynchMe(t);
            var lst = new List<SolPoint>(SolPointList.Value);
            foreach(var sp in lst) {
                _curr4Draw.SynchMeTo(sp);
                var t2 = sp.T;
                if(t2 > t)
                    break;
    
                Pos.Points.Add(new DataPoint(_curr4Draw.X,_curr4Draw.Y));
                Trg.Points.Add(new DataPoint(_curr4Draw.targFunc(t2).X,_curr4Draw.targFunc(t2).Y));
                
            }


            pm.Title = $"{t:0.###} s";
            pm.InvalidatePlot(true);
            //Thread.Sleep(1000);
        }


        public PlotModel GetNewModel(string title = "",string xname = "",string yname = "") {

            var m = new PlotModel { Title = title };
            m.PlotType = PlotType.Cartesian;
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
            return m;
        }
    }
}
