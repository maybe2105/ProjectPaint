using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Contract
{
    public abstract class IShape
    {
        public virtual string Name { get; set; }
        public double StrokeThickness { get; set; } = Options.strokeThickness;
        public System.Windows.Media.Color Color { get; set; } = Options.previewColor;
        public string DashStyle { get; set; } = "";
        public abstract void HandleStart(Point2D point);
        public abstract void HandleEnd(Point2D point);

        public abstract void HandleShiftMode();

        public abstract UIElement Draw();
    }
}
