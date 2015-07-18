using System;
using System.Web;

namespace ProjectFlx
{
    public interface FlxPageInterface
    {
        void PAGE_INIT();
        void PAGE_MAIN();
        void PAGE_TERMINATE();
    }
}