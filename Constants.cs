using BadukServer;

public class Constants
{

    public static List<TimeControlDto> timeControlsForMatch = [
      // TimeStandard.blitz
      new TimeControlDto(
    mainTimeSeconds: 30,
    incrementSeconds: 3,
    byoYomiTime: null
  ),
  new TimeControlDto(
      mainTimeSeconds: 30,
      incrementSeconds: null,
      byoYomiTime: new ByoYomiTime(byoYomis: 5, byoYomiSeconds: 10)),
  // TimeStandard.rapid
  new TimeControlDto(
    mainTimeSeconds: 5 * 60,
    incrementSeconds: 5,
    byoYomiTime: null
  ),
  new TimeControlDto(
      mainTimeSeconds: 5 * 60,
      incrementSeconds: null,
      byoYomiTime: new ByoYomiTime(byoYomis: 5, byoYomiSeconds: 30)),

  new TimeControlDto(
    mainTimeSeconds: 10 * 60,
    incrementSeconds: 10,
    byoYomiTime: null
  ),
  new TimeControlDto(
      mainTimeSeconds: 20 * 60,
      incrementSeconds: null,
      byoYomiTime: new ByoYomiTime(byoYomis: 5, byoYomiSeconds: 30)),
];
}