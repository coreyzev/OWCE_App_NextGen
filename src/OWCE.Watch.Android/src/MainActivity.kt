package app.owce.watch

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.wear.compose.material.*

/**
 * OWCE Wear OS watch face.
 *
 * Displays: current speed, top speed, battery %, estimated range.
 * Data is received from the phone via the OWCEDataListenerService.
 *
 * Uses Jetpack Compose for Wear OS (wear-compose-material).
 */
class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            OWCEWatchApp()
        }
    }
}

@Composable
fun OWCEWatchApp() {
    // Observe the shared state from the listener service
    val state by OWCEWatchState.state.collectAsState()

    MaterialTheme {
        Scaffold(
            vignette = { Vignette(vignettePosition = VignettePosition.TopAndBottom) }
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(Color.Black),
                contentAlignment = Alignment.Center
            ) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center,
                    modifier = Modifier.padding(8.dp)
                ) {
                    // Current Speed (large)
                    Text(
                        text = "%.1f".format(state.speedDisplay),
                        fontSize = 48.sp,
                        fontWeight = FontWeight.Bold,
                        color = if (state.isRiding) Color(0xFF00B4D8) else Color.Gray
                    )
                    Text(
                        text = state.unit.uppercase(),
                        fontSize = 12.sp,
                        color = Color.Gray
                    )

                    Spacer(modifier = Modifier.height(4.dp))

                    // Top Speed
                    Text(
                        text = "TOP %.1f".format(state.topSpeedDisplay),
                        fontSize = 13.sp,
                        color = Color(0xFFE74C3C)
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    // Battery + Range row
                    Row(
                        horizontalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(
                                text = "${state.batteryPercent}%",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = batteryColor(state.batteryPercent)
                            )
                            Text(text = "BATT", fontSize = 9.sp, color = Color.Gray)
                        }
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(
                                text = "%.1f".format(state.estimatedRangeMiles),
                                fontSize = 16.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = Color(0xFF00B4D8)
                            )
                            Text(text = "RANGE", fontSize = 9.sp, color = Color.Gray)
                        }
                    }
                }
            }
        }
    }
}

private fun batteryColor(percent: Int): Color = when {
    percent > 50 -> Color(0xFF2ECC71)
    percent > 20 -> Color(0xFFF39C12)
    else         -> Color(0xFFE74C3C)
}
