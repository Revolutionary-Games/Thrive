material Examples/SphereMappedRustySteel
{
	technique
	{
		pass
		{
			texture_unit
			{
				texture RustySteel.jpg
			}

			texture_unit
			{
				texture spheremap.png
				colour_op_ex add src_texture src_current
				colour_op_multipass_fallback one one
				env_map spherical
			}
		}
	}
}