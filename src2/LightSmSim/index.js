const sm  = new LightSm();
const buttonsDiv = document.getElementById("buttons-div");

for (const eventName in LightSm.EventId) {
    if (Object.hasOwnProperty.call(LightSm.EventId, eventName)) {
        const eventValue = LightSm.EventId[eventName];
        const button = document.createElement("button");
        button.innerText = eventName;
        button.onclick = () => {
            console.log(`=========== Dispatching event: ${eventName} ===========`);
            sm.dispatchEvent(eventValue);
        }
        buttonsDiv.appendChild(button);
    }
}

sm.start();