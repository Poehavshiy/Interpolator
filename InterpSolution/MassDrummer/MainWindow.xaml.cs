using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace MassDrummer {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        List<CheckBox> chBxes = new List<CheckBox>(20);
        List<IObservable<string>> chBxesStreams = new List<IObservable<string>>(20);
        IObservable<string> chBxesAllStreams;
        void FillListChBx() {
            chBxes.Add(chbxRo);
            chBxes.Add(chbxM);

            chBxes.Add(chbx1D);
            chBxes.Add(chbx1H);

            chBxes.Add(chbx2D1);
            chBxes.Add(chbx2D2);

            chBxes.Add(chbx3D1);
            chBxes.Add(chbx3D2);
            chBxes.Add(chbx3H);

            chBxes.Add(chbx4D);
            chBxes.Add(chbx4H1);
            chBxes.Add(chbx4H2);

            chBxes.Add(chbx5H);
            chBxes.Add(chbx5h);
            chBxes.Add(chbx5R);

            for(int i = 0; i < chBxes.Count; i++) {
                chBxesStreams.Add(
                    Observable.FromEventPattern<RoutedEventArgs>(chBxes[i],"Checked")
                    .Select(e => {
                        return (e.Sender as CheckBox).Name;
                    }));
            }
            chBxesAllStreams = Observable.Merge(chBxesStreams);

            chBxesAllStreams.Subscribe(uncheckall);

           // chBxesAllStreams.TakeLast()
        }

        Dictionary<string,TextBox> tb_dict = new Dictionary<string,TextBox>(20);
        List<IObservable<Tuple<string,double>>> tb_valsStreams = new List<IObservable<Tuple<string,double>>>(20);
        IObservable<Tuple<string,double>> tb_AllStreams;
        void fillTxtBxes() {
            foreach(TextBox tb in FindVisualChildren<TextBox>(this)) {
                tb_dict.Add(tb.Name,tb);
                tb_valsStreams.Add(
                    Observable.FromEventPattern<TextChangedEventArgs>(tb,"TextChanged")
                    .Select(e => {
                        var value = (e.Sender as TextBox).Text;
                        
                        double result;

                        //Try parsing in the current culture
                        if(!double.TryParse(value,System.Globalization.NumberStyles.Any,CultureInfo.CurrentCulture,out result) &&
                            //Then try in US english
                            !double.TryParse(value,System.Globalization.NumberStyles.Any,CultureInfo.GetCultureInfo("en-US"),out result) &&
                            //Then in neutral language
                            !double.TryParse(value,System.Globalization.NumberStyles.Any,CultureInfo.InvariantCulture,out result)) {
                            result = -1;
                        }

                        return new Tuple<string,double>((e.Sender as TextBox).Name,result);
                    })
                    .Where(tsd => tsd.Item2 >= 0d)
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .DistinctUntilChanged(tss => tss.Item2)
                    );
            }
            tb_AllStreams = Observable.Merge(tb_valsStreams);



        }


        void uncheckall(string exeptIt) {
            foreach(var cb in chBxes.Where(c => c.Name != exeptIt)) {
                cb.IsChecked = false;
            }
        }

        public MainWindow() {
            InitializeComponent();
            FillListChBx();
            
            var tbStream = Observable.FromEventPattern<SelectionChangedEventArgs>(tabControl,"SelectionChanged")
                .Where(e => e.Sender is TabControl)
                .Select(e => {
                    if(ti1 != null && ti1.IsSelected)
                        return 1;
                    if(ti2 != null && ti2.IsSelected)
                        return 2;
                    if(ti3 != null && ti3.IsSelected)
                        return 3;
                    if(ti4 != null && ti4.IsSelected)
                        return 4;
                    if(ti5 != null && ti5.IsSelected)
                        return 5;
                    return 0;
                });
            tbStream.Subscribe(recheckWhenTabCh);
            chBxesAllStreams.Subscribe(s => Title = s);


        }

        void recheckWhenTabCh(int tbi) {
            if(chbxRo.IsChecked == true || chbxM.IsChecked == true)
                return;
            switch(tbi) {
                case (1): {
                    chbx1D.IsChecked = true;
                    break;
                }
                case (2): {
                    chbx2D1.IsChecked = true;
                    break;
                }
                case (3): {
                    chbx3H.IsChecked = true;
                    break;
                }
                case (4): {
                    chbx4D.IsChecked = true;
                    break;
                }
                case (5): {
                    chbx5H.IsChecked = true;
                    break;
                }
                default:
                break;
            }

        }

        private void tabControl_SelectionChanged(object sender,SelectionChangedEventArgs e) {

        }

        private void chbxM_Checked(object sender,RoutedEventArgs e) {

        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
            if(depObj != null) {
                for(int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj,i);
                    if(child != null && child is T) {
                        yield return (T)child;
                    }

                    foreach(T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void textBox_Copy_TextChanged(object sender,TextChangedEventArgs e) {

        }

        private void Window_Loaded(object sender,RoutedEventArgs e) {
            fillTxtBxes();
            tb_AllStreams.ObserveOnDispatcher().Subscribe(tss => Title = $"{tss.Item1}   val = {tss.Item2}");
        }
    }
}
