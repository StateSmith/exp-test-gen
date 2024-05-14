// Autogenerated with StateSmith 0.9.12-alpha.
// Algorithm: Balanced1. See https://github.com/StateSmith/StateSmith/wiki/Algorithms

// Generated state machine
export class LightSm
{
    static EventId = 
    {
        DIM : 0,
        INCREASE : 1,
        OFF : 2,
    }
    static { Object.freeze(this.EventId); }
    
    static EventIdCount = 3;
    static { Object.freeze(this.EventIdCount); }
    
    static StateId = 
    {
        ROOT : 0,
        OFF : 1,
        ON_GROUP : 2,
        ON_HOT : 3,
        ON1 : 4,
        ON2 : 5,
    }
    static { Object.freeze(this.StateId); }
    
    static StateIdCount = 6;
    static { Object.freeze(this.StateIdCount); }
    
    // Used internally by state machine. Feel free to inspect, but don't modify.
    stateId;
    
    // Used internally by state machine. Don't modify.
    #ancestorEventHandler;
    
    // Used internally by state machine. Don't modify.
    #currentEventHandlers = Array(LightSm.EventIdCount).fill(undefined);
    
    // Used internally by state machine. Don't modify.
    #currentStateExitHandler;
    
    // Variables. Can be used for inputs, outputs, user variables...
    vars = {
        count: 0 // variable for state machine
    };
    
    // Starts the state machine. Must be called before dispatching events. Not thread safe.
    start()
    {
        this.#ROOT_enter();
        // ROOT behavior
        // uml: TransitionTo(ROOT.<InitialState>)
        {
            // Step 1: Exit states until we reach `ROOT` state (Least Common Ancestor for transition). Already at LCA, no exiting required.
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ROOT.<InitialState>`.
            // ROOT.<InitialState> is a pseudo state and cannot have an `enter` trigger.
            
            // ROOT.<InitialState> behavior
            // uml: TransitionTo(OFF)
            {
                // Step 1: Exit states until we reach `ROOT` state (Least Common Ancestor for transition). Already at LCA, no exiting required.
                
                // Step 2: Transition action: ``.
                
                // Step 3: Enter/move towards transition target `OFF`.
                this.#OFF_enter();
                
                // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
                this.stateId = LightSm.StateId.OFF;
                // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
                return;
            } // end of behavior for ROOT.<InitialState>
        } // end of behavior for ROOT
    }
    
    // Dispatches an event to the state machine. Not thread safe.
    dispatchEvent(eventId)
    {
        let behaviorFunc = this.#currentEventHandlers[eventId];
        
        while (behaviorFunc != null)
        {
            this.#ancestorEventHandler = null;
            behaviorFunc.call(this);
            behaviorFunc = this.#ancestorEventHandler;
        }
    }
    
    // This function is used when StateSmith doesn't know what the active leaf state is at
    // compile time due to sub states or when multiple states need to be exited.
    #exitUpToStateHandler(desiredStateExitHandler)
    {
        while (this.#currentStateExitHandler != desiredStateExitHandler)
        {
            this.#currentStateExitHandler.call(this);
        }
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state ROOT
    ////////////////////////////////////////////////////////////////////////////////
    
    #ROOT_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#ROOT_exit;
    }
    
    #ROOT_exit()
    {
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state OFF
    ////////////////////////////////////////////////////////////////////////////////
    
    #OFF_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#OFF_exit;
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = this.#OFF_increase;
        
        // OFF behavior
        // uml: enter / { println("OFF"); }
        {
            // Step 1: execute action `println("OFF");`
            println("OFF");
        } // end of behavior for OFF
    }
    
    #OFF_exit()
    {
        // adjust function pointers for this state's exit
        this.#currentStateExitHandler = this.#ROOT_exit;
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = null;  // no ancestor listens to this event
    }
    
    #OFF_increase()
    {
        // No ancestor state handles `increase` event.
        
        // OFF behavior
        // uml: INCREASE TransitionTo(ON1)
        {
            // Step 1: Exit states until we reach `ROOT` state (Least Common Ancestor for transition).
            this.#OFF_exit();
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ON1`.
            this.#ON_GROUP_enter();
            this.#ON1_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.ON1;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for OFF
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state ON_GROUP
    ////////////////////////////////////////////////////////////////////////////////
    
    #ON_GROUP_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#ON_GROUP_exit;
        this.#currentEventHandlers[LightSm.EventId.OFF] = this.#ON_GROUP_off;
    }
    
    #ON_GROUP_exit()
    {
        // adjust function pointers for this state's exit
        this.#currentStateExitHandler = this.#ROOT_exit;
        this.#currentEventHandlers[LightSm.EventId.OFF] = null;  // no ancestor listens to this event
    }
    
    #ON_GROUP_off()
    {
        // No ancestor state handles `off` event.
        
        // ON_GROUP behavior
        // uml: OFF TransitionTo(OFF)
        {
            // Step 1: Exit states until we reach `ROOT` state (Least Common Ancestor for transition).
            this.#exitUpToStateHandler(this.#ROOT_exit);
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `OFF`.
            this.#OFF_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.OFF;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON_GROUP
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state ON_HOT
    ////////////////////////////////////////////////////////////////////////////////
    
    #ON_HOT_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#ON_HOT_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = this.#ON_HOT_dim;
        
        // ON_HOT behavior
        // uml: enter / { light_red(); }
        {
            // Step 1: execute action `light_red();`
            light_red();
        } // end of behavior for ON_HOT
    }
    
    #ON_HOT_exit()
    {
        // adjust function pointers for this state's exit
        this.#currentStateExitHandler = this.#ON_GROUP_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = null;  // no ancestor listens to this event
    }
    
    #ON_HOT_dim()
    {
        // No ancestor state handles `dim` event.
        
        // ON_HOT behavior
        // uml: DIM TransitionTo(ON2)
        {
            // Step 1: Exit states until we reach `ON_GROUP` state (Least Common Ancestor for transition).
            this.#ON_HOT_exit();
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ON2`.
            this.#ON2_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.ON2;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON_HOT
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state ON1
    ////////////////////////////////////////////////////////////////////////////////
    
    #ON1_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#ON1_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = this.#ON1_dim;
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = this.#ON1_increase;
        
        // ON1 behavior
        // uml: enter / { light_blue(); }
        {
            // Step 1: execute action `light_blue();`
            light_blue();
        } // end of behavior for ON1
    }
    
    #ON1_exit()
    {
        // adjust function pointers for this state's exit
        this.#currentStateExitHandler = this.#ON_GROUP_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = null;  // no ancestor listens to this event
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = null;  // no ancestor listens to this event
    }
    
    #ON1_dim()
    {
        // No ancestor state handles `dim` event.
        
        // ON1 behavior
        // uml: DIM TransitionTo(OFF)
        {
            // Step 1: Exit states until we reach `ROOT` state (Least Common Ancestor for transition).
            this.#exitUpToStateHandler(this.#ROOT_exit);
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `OFF`.
            this.#OFF_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.OFF;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON1
    }
    
    #ON1_increase()
    {
        // No ancestor state handles `increase` event.
        
        // ON1 behavior
        // uml: INCREASE TransitionTo(ON2)
        {
            // Step 1: Exit states until we reach `ON_GROUP` state (Least Common Ancestor for transition).
            this.#ON1_exit();
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ON2`.
            this.#ON2_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.ON2;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON1
    }
    
    
    ////////////////////////////////////////////////////////////////////////////////
    // event handlers for state ON2
    ////////////////////////////////////////////////////////////////////////////////
    
    #ON2_enter()
    {
        // setup trigger/event handlers
        this.#currentStateExitHandler = this.#ON2_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = this.#ON2_dim;
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = this.#ON2_increase;
        
        // ON2 behavior
        // uml: enter / { light_yellow(); }
        {
            // Step 1: execute action `light_yellow();`
            light_yellow();
        } // end of behavior for ON2
        
        // ON2 behavior
        // uml: enter / { count=0; }
        {
            // Step 1: execute action `count=0;`
            this.vars.count=0;
        } // end of behavior for ON2
    }
    
    #ON2_exit()
    {
        // adjust function pointers for this state's exit
        this.#currentStateExitHandler = this.#ON_GROUP_exit;
        this.#currentEventHandlers[LightSm.EventId.DIM] = null;  // no ancestor listens to this event
        this.#currentEventHandlers[LightSm.EventId.INCREASE] = null;  // no ancestor listens to this event
    }
    
    #ON2_dim()
    {
        // No ancestor state handles `dim` event.
        
        // ON2 behavior
        // uml: DIM TransitionTo(ON1)
        {
            // Step 1: Exit states until we reach `ON_GROUP` state (Least Common Ancestor for transition).
            this.#ON2_exit();
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ON1`.
            this.#ON1_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.ON1;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON2
    }
    
    #ON2_increase()
    {
        // No ancestor state handles `increase` event.
        
        // ON2 behavior
        // uml: 1. INCREASE / { count++; }
        {
            // Step 1: execute action `count++;`
            this.vars.count++;
            
            // Step 2: determine if ancestor gets to handle event next.
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
        } // end of behavior for ON2
        
        // ON2 behavior
        // uml: 2. INCREASE [count >= 3] TransitionTo(ON_HOT)
        if (this.vars.count >= 3)
        {
            // Step 1: Exit states until we reach `ON_GROUP` state (Least Common Ancestor for transition).
            this.#ON2_exit();
            
            // Step 2: Transition action: ``.
            
            // Step 3: Enter/move towards transition target `ON_HOT`.
            this.#ON_HOT_enter();
            
            // Step 4: complete transition. Ends event dispatch. No other behaviors are checked.
            this.stateId = LightSm.StateId.ON_HOT;
            // No ancestor handles event. Can skip nulling `ancestorEventHandler`.
            return;
        } // end of behavior for ON2
    }
    
    // Thread safe.
    static stateIdToString(id)
    {
        switch (id)
        {
            case LightSm.StateId.ROOT: return "ROOT";
            case LightSm.StateId.OFF: return "OFF";
            case LightSm.StateId.ON_GROUP: return "ON_GROUP";
            case LightSm.StateId.ON_HOT: return "ON_HOT";
            case LightSm.StateId.ON1: return "ON1";
            case LightSm.StateId.ON2: return "ON2";
            default: return "?";
        }
    }
    
    // Thread safe.
    static eventIdToString(id)
    {
        switch (id)
        {
            case LightSm.EventId.DIM: return "DIM";
            case LightSm.EventId.INCREASE: return "INCREASE";
            case LightSm.EventId.OFF: return "OFF";
            default: return "?";
        }
    }
}
