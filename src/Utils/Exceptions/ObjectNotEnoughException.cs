using System;
using System.Collections.Generic;
using System.Text;

namespace TheColdWorld.Utils.Exceptions;

public class ObjectNotEnoughException:Exception
{
    public ObjectNotEnoughException() :base("Object is not enough for use"){ }
}
