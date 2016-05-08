using System;
using Microsoft.SqlServer.Server;

namespace VMLab.Model
{
    public class GuestVMPoweredOffException : Exception
    {
        public GuestVMPoweredOffException(string message) : base(message) { }
    }

    public class VMXDoesntExistException : Exception
    {
        public string VMXPath { get; }

        public VMXDoesntExistException(string message, string vmxpath) : base(message)
        {
            VMXPath = vmxpath;
        }
    }

    public class FileDoesntExistInGuest : Exception
    {
        public string VMXPath { get; }
        public string GuestPath { get; }

        public FileDoesntExistInGuest(string message, string vmxpath, string guestpath) : base(message)
        {
            VMXPath = vmxpath;
            GuestPath = guestpath;
        }
}

    public class VMXAlreadyExistsException : Exception
    {
        public string VMXPath { get; }

        public VMXAlreadyExistsException(string message, string vmxpath) : base(message)
        {
            VMXPath = vmxpath;
        }
    }

    public class SnapshotDoesntExistException : Exception
    {
        public string VMXPath { get; }
        public string Snapshot { get; }

        public SnapshotDoesntExistException(string message, string vmxpath, string snapshot) : base(message)
        {
            VMXPath = vmxpath;
            Snapshot = snapshot;
        }
    }

    public class BadGuestCredentialsException : Exception
    {
        public IVMCredential[] Credentials { get; }

        public BadGuestCredentialsException(string message, IVMCredential[] credentials) : base(message)
        {
            Credentials = credentials;
        }
    }

    public class VMRunFailedToRunException : Exception
    {
        public string VMRunResults { get; }

        public VMRunFailedToRunException(string message, string vmrunresult) : base($"{message} VMRun: {vmrunresult}")
        {
            
            VMRunResults = vmrunresult;
        }
    }

    public class VDiskManException : Exception
    {
        public string Results { get;  }

        public VDiskManException(string message, string results) : base(message)
        {
            Results = results;
        }
    }

    public class BadBusTypeException : Exception
    {
        
        public string BusType { get; }

        public BadBusTypeException(string message, string bustype) : base(message)
        {
            BusType = bustype;
        }
    }

    public class NodeHandlerDoesntExistException : Exception
    {
        public string HandlerType { get; }

        public NodeHandlerDoesntExistException(string message, string handler) : base(message)
        {
            HandlerType = handler;
        }
    }

    public class NullActionException : Exception
    {
        
    }

    public class MissingVMLabFileException : Exception
    {
        public MissingVMLabFileException(string message) : base(message)
        {
            
        }
    }

    public class InvalidNodeParametersException : Exception
    {
        public object Settings { get; }

        public InvalidNodeParametersException(string message, object settings) : base(message)
        {
            Settings = settings;
        }
    }

    public class ComponentDoesntExist : Exception
    {
        public string ComponentName { get; }

        public ComponentDoesntExist(string message, string name) : base(message)
        {
            ComponentName = name;
        }
    }

    public class IdempotentActionAlreadyExists : Exception
    {
        public IdempotentActionAlreadyExists(string message) : base(message)
        {
            
        }
    }

    public class IdempotentActionPropertyException : Exception
    {
        public IdempotentActionPropertyException(string message) : base(message)
        {
            
        }
    }

    public class IdempotentActionDoestnExist : Exception
    {
        public IdempotentActionDoestnExist(string message) : base(message)
        {
            
        }
    }

    public class IdempotentActionNotConfigured : Exception
    {
        public IdempotentActionNotConfigured(string message) : base(message)
        {
            
        }
    }

    public class NonExistingLabLibraryException : Exception
    {
        public NonExistingLabLibraryException(string message) : base(message)
        {
            
        }
    }

    public class DuplicateRepositoryException : Exception
    {
        public DuplicateRepositoryException(string message) : base(message)
        {
            
        }
    }
}
