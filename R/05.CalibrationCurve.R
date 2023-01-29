warning("Please manually run the code line by line and save the image from the preview box.")
source("00.Functions.R")
CheckPackage(c("survival", "rms"))

# We need some output from the nomogram
source("04.Nomogram.R")

drawCal = function(data, time, m){
  f <- cph(Surv(Time,Status) ~ Age+Gender+M+T+`TIL(%)`, data = data, x=T, y = T, surv = T, time.inc = time)
  cal <- calibrate(f, 
                    cmethod='KM', 
                    method="boot", 
                    u=time,
                    m=m,
                    B=1000)
  plot(cal,lwd=2,lty=1,
       conf.int=T,
       errbar.col="blue",
       col="red",
       xlab=paste0("Nomogram-Predicted Probability of ",time ,"-Year OS"),
       ylab=paste0("Actual ",time ,"-Year OS (proportion)"),
       subtitles = F)
}

drawCal(data.nomo, 1, 100)
drawCal(data.nomo, 2, 100)
drawCal(data.nomo, 5, 100)

# XJH
data.nomo.XJH = read_excel("Data/OrginalData.XJH.xlsx")
data.nomo.XJH$Age = ifelse(data.nomo.XJH$Age < 65, "<65", ">=65")
ret = c("Age", "Gender", "M", "T", "prec.TIL", "Time", "Status")
data.nomo.XJH = data.nomo.XJH[,colnames(data.nomo.XJH) %in% ret]

i = 0
for (variable in colnames(data.nomo.XJH)) {
  i = i + 1
  if (colnames(data.nomo.XJH)[i] == "prec.TIL") {
    colnames(data.nomo.XJH)[i] = "TIL(%)"
    break
  }
}

drawCal(data.nomo.XJH, 1, 16)
drawCal(data.nomo.XJH, 2, 16)
drawCal(data.nomo.XJH, 5, 16)