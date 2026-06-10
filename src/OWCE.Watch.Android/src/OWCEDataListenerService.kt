package app.owce.watch

import com.google.android.gms.wearable.DataEvent
import com.google.android.gms.wearable.DataEventBuffer
import com.google.android.gms.wearable.DataMapItem
import com.google.android.gms.wearable.WearableListenerService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

/**
 * Shared state for the OWCE watch app.
 * Updated by OWCEDataListenerService, read by the Compose UI.
 */
data class WatchTelemetryState(
    val speedDisplay: Float = 0f,
    val topSpeedDisplay: Float = 0f,
    val batteryPercent: Int = 0,
    val estimatedRangeMiles: Float = 0f,
    val unit: String = "mph",
    val isRiding: Boolean = false,
)

object OWCEWatchState {
    private val _state = MutableStateFlow(WatchTelemetryState())
    val state: StateFlow<WatchTelemetryState> = _state

    fun update(new: WatchTelemetryState) { _state.value = new }
}

/**
 * Wearable DataListener that receives telemetry from the phone app.
 * Declared in AndroidManifest.xml with the BIND_LISTENER permission.
 *
 * Data path: /owce/telemetry
 * Keys: speed (float), topSpeed (float), battery (int), range (float),
 *       unit (string), riding (boolean)
 */
class OWCEDataListenerService : WearableListenerService() {

    override fun onDataChanged(dataEvents: DataEventBuffer) {
        for (event in dataEvents) {
            if (event.type == DataEvent.TYPE_CHANGED &&
                event.dataItem.uri.path == "/owce/telemetry"
            ) {
                val dataMap = DataMapItem.fromDataItem(event.dataItem).dataMap

                val speedMph    = dataMap.getFloat("speed", 0f)
                val topSpeedMph = dataMap.getFloat("topSpeed", 0f)
                val battery     = dataMap.getInt("battery", 0)
                val range       = dataMap.getFloat("range", 0f)
                val unit        = dataMap.getString("unit", "mph")
                val isRiding    = dataMap.getBoolean("riding", false)

                // Convert to display unit
                val isMetric = unit == "km/h"
                val speedDisplay    = if (isMetric) speedMph * 1.60934f else speedMph
                val topSpeedDisplay = if (isMetric) topSpeedMph * 1.60934f else topSpeedMph

                OWCEWatchState.update(
                    WatchTelemetryState(
                        speedDisplay    = speedDisplay,
                        topSpeedDisplay = topSpeedDisplay,
                        batteryPercent  = battery,
                        estimatedRangeMiles = range,
                        unit            = unit,
                        isRiding        = isRiding,
                    )
                )
            }
        }
        dataEvents.release()
    }
}
