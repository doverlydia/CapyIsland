import React from 'react';
import {Composition} from 'remotion';
import {SnowMelt, SNOW_MELT_FRAMES} from './SnowMelt';
import {Trailer15, TRAILER15_FRAMES} from './Trailer15';
import {trailerFrames, TrailerGameplay} from './TrailerGameplay';

export const RemotionRoot: React.FC = () => (
	<>
		<Composition id="Trailer" component={TrailerGameplay} defaultProps={{cut: 'full' as const}} durationInFrames={trailerFrames('full')} fps={30} width={1920} height={1080} />
		<Composition id="TrailerShort" component={TrailerGameplay} defaultProps={{cut: 'short' as const}} durationInFrames={trailerFrames('short')} fps={30} width={1920} height={1080} />
		<Composition id="Trailer15" component={Trailer15} durationInFrames={TRAILER15_FRAMES} fps={30} width={1920} height={1080} />
		<Composition id="SnowMelt" component={SnowMelt} durationInFrames={SNOW_MELT_FRAMES} fps={30} width={1920} height={1080} />
	</>
);
