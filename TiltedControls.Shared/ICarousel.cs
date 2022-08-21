using System;
using System.Collections.Generic;
using System.Text;
using static TiltedControls.Common;

namespace TiltedControls
{
    public interface ICarousel
    {
        bool AreItemsLoaded { get; set; }
        CarouselTypes CarouselType { get; set; }
        WheelAlignments WheelAlignment { get; set; }

        void ChangeSelection(bool reverse);
        void AnimateSelection();
    }
}
